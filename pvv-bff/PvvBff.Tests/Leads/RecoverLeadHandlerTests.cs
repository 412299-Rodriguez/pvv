using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Leads.Recovery;
using PvvBff.Domain.Leads;

namespace PvvBff.Tests.Leads;

/// <summary>
/// Recovery writes to a real person who did not ask to be written to. The rules that
/// keep that defensible — one message only, only to someone who left an address, only
/// within the operator's own tenant — are enforced here rather than in the panel,
/// because the panel is a convenience and this is the boundary.
/// </summary>
public class RecoverLeadHandlerTests
{
    private const string FlowId = "flow-1";
    private const string CompanyToken = "company-token";
    private const string Operator = "admin@segucor.com";

    private readonly Mock<ILeadRecoveryStore> _store = new();
    private readonly Mock<ICompanyConfigReader> _config = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<IPortalLinkBuilder> _links = new();
    private readonly RecoverLeadHandler _handler;

    public RecoverLeadHandlerTests()
    {
        _links.Setup(l => l.BuildPortalUrl(It.IsAny<string>())).Returns("https://portal.test/?c=token");
        _config.Setup(c => c.GetByHashAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((object?)null);
        _email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailResult(true));

        _handler = new RecoverLeadHandler(
            _store.Object, _config.Object, _email.Object, _links.Object,
            NullLogger<RecoverLeadHandler>.Instance);
    }

    // ---- Preconditions ---------------------------------------------------------

    [Fact]
    public async Task A_lead_outside_the_operators_company_simply_does_not_exist()
    {
        // The scope is part of the lookup, so a cross-tenant flow id is indistinguishable
        // from a missing one — there is no path that reads it and then decides.
        _store.Setup(s => s.GetScopedAsync(FlowId, CompanyToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead?)null);

        var result = await Handle();

        Assert.False(result.Sent);
        Assert.Equal("not_found", result.Status);
        VerifyNoEmailSent();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_lead_that_left_no_address_is_not_recoverable(string? email)
    {
        // The rule that defines the feature's scope: no address, no recovery.
        GivenLead(l => l.ContactEmail = email);

        var result = await Handle();

        Assert.Equal("no_contact", result.Status);
        VerifyNoEmailSent();
        _store.Verify(
            s => s.TryClaimRecoveryAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task A_lead_already_recovered_is_not_written_to_twice()
    {
        GivenLead();
        GivenClaim(won: false);

        var result = await Handle();

        Assert.False(result.Sent);
        Assert.Equal("already_recovered", result.Status);
        VerifyNoEmailSent();
    }

    // ---- The send --------------------------------------------------------------

    [Fact]
    public async Task A_recoverable_lead_gets_exactly_one_email()
    {
        GivenLead();
        GivenClaim(won: true);

        var result = await Handle();

        Assert.True(result.Sent);
        Assert.Equal("sent", result.Status);
        _email.Verify(e => e.SendAsync(
            It.Is<EmailMessage>(m => m.To == "juan@example.com"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task The_lead_is_claimed_before_the_message_goes_out()
    {
        // Two operators pressing the button at the same instant is the ordinary case:
        // claiming after sending would let both of them send.
        GivenLead();
        var sequence = new List<string>();
        _store.Setup(s => s.TryClaimRecoveryAsync(FlowId, It.IsAny<DateTime>(), Operator, It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("claim"))
            .ReturnsAsync(true);
        _email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback(() => sequence.Add("send"))
            .ReturnsAsync(new EmailResult(true));

        await Handle();

        Assert.Equal(["claim", "send"], sequence);
    }

    [Fact]
    public async Task The_claim_records_which_operator_pressed_the_button()
    {
        GivenLead();
        GivenClaim(won: true);

        await Handle();

        _store.Verify(
            s => s.TryClaimRecoveryAsync(FlowId, It.IsAny<DateTime>(), Operator, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ---- Failure ---------------------------------------------------------------

    [Fact]
    public async Task A_send_that_never_left_hands_the_claim_back()
    {
        // Otherwise a provider outage permanently marks the lead as contacted by an
        // email nobody received: no message and no second chance.
        GivenLead();
        GivenClaim(won: true);
        _email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailResult(false, "525 Unauthorized IP address"));

        var result = await Handle();

        Assert.False(result.Sent);
        Assert.Equal("send_failed", result.Status);
        Assert.Equal("525 Unauthorized IP address", result.Error);
        _store.Verify(s => s.ReleaseRecoveryClaimAsync(FlowId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_successful_send_keeps_the_claim()
    {
        GivenLead();
        GivenClaim(won: true);

        await Handle();

        _store.Verify(
            s => s.ReleaseRecoveryClaimAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Branding --------------------------------------------------------------

    [Fact]
    public async Task A_company_that_never_customised_its_portal_still_gets_a_usable_email()
    {
        // Both config reads answer null — which is what pvv-config returns for a
        // tenant that has no RECOVERY_EMAIL_CONFIG yet.
        GivenLead();
        GivenClaim(won: true);

        EmailMessage? sent = null;
        _email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((m, _) => sent = m)
            .ReturnsAsync(new EmailResult(true));

        await Handle();

        Assert.NotNull(sent);
        Assert.NotEmpty(sent.Subject);
        Assert.NotEmpty(sent.HtmlBody);
        Assert.Contains("tu aseguradora", sent.Subject);
    }

    [Fact]
    public async Task The_portal_link_is_built_for_the_operators_own_company()
    {
        GivenLead();
        GivenClaim(won: true);

        await Handle();

        _links.Verify(l => l.BuildPortalUrl(CompanyToken), Times.Once);
    }

    // ---- Helpers ---------------------------------------------------------------

    private Task<RecoverLeadResult> Handle() =>
        _handler.Handle(new RecoverLeadCommand(FlowId, CompanyToken, Operator), CancellationToken.None);

    private void GivenLead(Action<Lead>? customize = null)
    {
        var lead = new Lead
        {
            Id = FlowId,
            CompanyToken = CompanyToken,
            ContactEmail = "juan@example.com",
            Status = LeadStatus.Abandoned,
        };
        lead.Steps.Step1.Plate = "AA001BB";
        lead.Steps.Step2.FirstName = "Juan";

        customize?.Invoke(lead);

        _store.Setup(s => s.GetScopedAsync(FlowId, CompanyToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(lead);
    }

    private void GivenClaim(bool won) =>
        _store.Setup(s => s.TryClaimRecoveryAsync(
                FlowId, It.IsAny<DateTime>(), Operator, It.IsAny<CancellationToken>()))
            .ReturnsAsync(won);

    private void VerifyNoEmailSent() =>
        _email.Verify(
            e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
}
