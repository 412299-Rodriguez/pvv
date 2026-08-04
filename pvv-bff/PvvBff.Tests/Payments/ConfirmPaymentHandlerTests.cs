using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Leads;
using PvvBff.Application.Payments;
using PvvBff.Domain.Payments;

namespace PvvBff.Tests.Payments;

/// <summary>
/// Three paths can confirm the same payment — the webhook, the result page's sync
/// and the background reconciliation — and publishing twice means emitting two
/// policies for one sale. The other half of the story is the opposite mistake:
/// treating a payment that is merely pending as a refusal, which throws away a
/// sale that was about to succeed.
/// </summary>
public class ConfirmPaymentHandlerTests
{
    private readonly Mock<IPaymentRepository> _repository = new();
    private readonly Mock<IEmissionPublisher> _publisher = new();
    private readonly Mock<ILeadProjectionService> _leads = new();
    private readonly ConfirmPaymentHandler _handler;

    public ConfirmPaymentHandlerTests()
    {
        _handler = new ConfirmPaymentHandler(
            _repository.Object, _publisher.Object, _leads.Object,
            NullLogger<ConfirmPaymentHandler>.Instance);
    }

    // ---- Nothing to act on -----------------------------------------------------

    [Fact]
    public async Task A_notification_for_an_unknown_transaction_emits_nothing()
    {
        _repository.Setup(r => r.GetAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction?)null);

        var result = await Handle(new ConfirmPaymentCommand("missing", PaymentOutcome.Approved));

        Assert.False(result.Found);
        Assert.Equal("not_found", result.Status);
        VerifyNothingPublished();
    }

    [Fact]
    public async Task A_transaction_already_confirmed_is_not_emitted_a_second_time()
    {
        GivenTransaction(t => t.Status = PaymentStatus.Confirmed);

        var result = await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Approved));

        Assert.Equal("already_confirmed", result.Status);
        Assert.False(result.Published);
        VerifyNothingPublished();
    }

    // ---- Pending is not a refusal ----------------------------------------------

    [Fact]
    public async Task A_pending_payment_decides_nothing_about_the_sale()
    {
        // Cash coupons, transfers and fraud review all arrive as "pending". The
        // pre-HU-11 code read anything that was not "approved" as a failure.
        GivenTransaction();

        var result = await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Pending));

        Assert.Equal("pending", result.Status);
        Assert.True(result.Found);
        VerifyNothingPublished();
        _repository.Verify(r => r.UpdateAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_pending_payment_records_its_deadline_so_the_sweep_leaves_it_alone()
    {
        // Without this the abandonment sweep discards a sale half an hour into a
        // coupon the buyer still has weeks to pay.
        GivenTransaction();
        var deadline = DateTime.UtcNow.AddDays(21);

        await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Pending, "mp-999", deadline));

        _repository.Verify(
            r => r.MarkPendingPaymentAsync("tx-1", "mp-999", deadline, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task A_pending_outcome_with_no_payment_behind_it_writes_nothing()
    {
        GivenTransaction();

        await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Pending));

        _repository.Verify(
            r => r.MarkPendingPaymentAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---- Rejection -------------------------------------------------------------

    [Fact]
    public async Task A_rejected_payment_fails_the_transaction_and_tells_the_funnel()
    {
        var transaction = GivenTransaction();

        var result = await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Rejected, "mp-42"));

        Assert.Equal("failed", result.Status);
        Assert.Equal(PaymentStatus.Failed, transaction.Status);
        Assert.Equal("mp-42", transaction.ProviderPaymentId);
        VerifyNothingPublished();
        VerifyLeadEvent(LeadEventNames.PaymentRejected);
    }

    [Fact]
    public async Task A_rejection_without_an_id_does_not_erase_the_one_already_recorded()
    {
        var transaction = GivenTransaction(t => t.ProviderPaymentId = "mp-original");

        await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Rejected));

        Assert.Equal("mp-original", transaction.ProviderPaymentId);
    }

    // ---- Approval --------------------------------------------------------------

    [Fact]
    public async Task The_first_approval_confirms_the_payment_and_queues_the_emission()
    {
        var transaction = GivenTransaction(t =>
        {
            t.BudgetId = "budget-7";
            t.Amount = 28000m;
            t.CompanyToken = "company-token";
            t.SessionId = "session-9";
        });
        GivenConfirmationWon();

        var result = await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Approved, "mp-77"));

        Assert.True(result.Published);
        Assert.Equal("confirmed", result.Status);
        Assert.Equal(PaymentStatus.Confirmed, transaction.Status);
        _publisher.Verify(
            p => p.PublishAsync(
                It.Is<EmissionMessage>(m =>
                    m.TransactionId == "tx-1" &&
                    m.BudgetId == "budget-7" &&
                    m.CompanyToken == "company-token" &&
                    m.SessionId == "session-9" &&
                    m.Amount == 28000m),
                It.IsAny<CancellationToken>()),
            Times.Once);
        VerifyLeadEvent(LeadEventNames.PaymentConfirmed);
    }

    [Fact]
    public async Task The_provider_payment_id_is_stamped_together_with_the_confirmation()
    {
        // It is the only link from an issued policy back to the payment that paid
        // for it — where every refund and chargeback question starts.
        GivenTransaction();
        GivenConfirmationWon();

        await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Approved, "mp-77"));

        _repository.Verify(
            r => r.TryMarkConfirmedAsync("tx-1", It.IsAny<DateTime>(), "mp-77", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Losing_the_race_to_confirm_publishes_nothing()
    {
        // THE test of this handler. The fast path above is a cheap read that two
        // concurrent callers can both pass; only the compare-and-set decides, and
        // the loser must stay quiet or the buyer gets a second policy.
        GivenTransaction();
        _repository.Setup(r => r.TryMarkConfirmedAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Approved, "mp-77"));

        Assert.Equal("already_confirmed", result.Status);
        Assert.False(result.Published);
        VerifyNothingPublished();
    }

    [Fact]
    public async Task A_payment_that_belongs_to_no_flow_is_still_emitted()
    {
        // The lead is a reporting concern; a missing one must not cost a policy.
        GivenTransaction(t => t.FlowId = null);
        GivenConfirmationWon();

        var result = await Handle(new ConfirmPaymentCommand("tx-1", PaymentOutcome.Approved));

        Assert.True(result.Published);
        _leads.Verify(l => l.ProjectAsync(It.IsAny<LeadEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Helpers ---------------------------------------------------------------

    private Task<ConfirmPaymentResult> Handle(ConfirmPaymentCommand command) =>
        _handler.Handle(command, CancellationToken.None);

    private PaymentTransaction GivenTransaction(Action<PaymentTransaction>? customize = null)
    {
        var transaction = new PaymentTransaction
        {
            Id = "tx-1",
            FlowId = "flow-1",
            BudgetId = "budget-1",
            CompanyToken = "company-token",
            SessionId = "session-1",
            Amount = 15000m,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
        };

        customize?.Invoke(transaction);

        _repository.Setup(r => r.GetAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        return transaction;
    }

    private void GivenConfirmationWon() =>
        _repository.Setup(r => r.TryMarkConfirmedAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

    private void VerifyNothingPublished() =>
        _publisher.Verify(
            p => p.PublishAsync(It.IsAny<EmissionMessage>(), It.IsAny<CancellationToken>()), Times.Never);

    private void VerifyLeadEvent(string eventName) =>
        _leads.Verify(
            l => l.ProjectAsync(It.Is<LeadEvent>(e => e.Name == eventName), It.IsAny<CancellationToken>()),
            Times.Once);
}
