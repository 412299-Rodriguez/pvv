using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Leads;
using PvvBff.Domain.Leads;

namespace PvvBff.Tests.Leads;

/// <summary>
/// The single event→funnel mapping, shared by five producers. Everything the admin
/// dashboard reports — how far people get, where they stall, who is worth an email —
/// is derived here, so a wrong mapping does not break a screen: it quietly reports
/// the wrong business reality.
/// </summary>
public class LeadProjectionServiceTests
{
    private readonly Mock<ILeadStore> _leads = new();
    private readonly Mock<IEventLogStore> _eventLogs = new();
    private readonly LeadProjectionService _service;

    public LeadProjectionServiceTests()
    {
        _service = new LeadProjectionService(
            _leads.Object, _eventLogs.Object, NullLogger<LeadProjectionService>.Instance);
    }

    // ---- Attribution -----------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_event_without_a_flow_cannot_be_attributed_and_is_dropped(string flowId)
    {
        await _service.ProjectAsync(Event(LeadEventNames.PlateEntered, flowId: flowId), CancellationToken.None);

        _eventLogs.Verify(e => e.AppendAsync(It.IsAny<EventLogEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _leads.Verify(l => l.ApplyAsync(It.IsAny<LeadProjection>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Every_projected_event_is_also_appended_to_the_audit_log()
    {
        await _service.ProjectAsync(Event(LeadEventNames.SessionStart), CancellationToken.None);

        _eventLogs.Verify(
            e => e.AppendAsync(It.Is<EventLogEntry>(x => x.Name == LeadEventNames.SessionStart), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task An_unknown_event_touches_no_funnel_field()
    {
        // The vocabulary is closed; an unrecognised name must not invent state.
        var projection = await Project(Event("something_else"));

        Assert.Empty(projection.Fields);
        Assert.Null(projection.MinLastStep);
        Assert.Null(projection.Status);
    }

    // ---- Step semantics: reached vs finished -----------------------------------

    [Fact]
    public async Task Entering_a_plate_reaches_step_one_without_completing_it()
    {
        var projection = await Project(Event(
            LeadEventNames.PlateEntered, payload: new() { ["plate"] = "AA001BB" }));

        Assert.Equal(1, projection.MinLastStep);
        Assert.Equal("started", projection.Fields["Steps.Step1.Status"]);
        Assert.Equal("AA001BB", projection.Fields["Steps.Step1.Plate"]);
    }

    [Fact]
    public async Task Validating_the_plate_completes_step_one()
    {
        var projection = await Project(Event(
            LeadEventNames.PlateValidated,
            payload: new() { ["plate"] = "AA001BB", ["vehicleTitle"] = "Toyota Etios 2021" }));

        Assert.Equal(1, projection.MinLastStep);
        Assert.Equal("completed", projection.Fields["Steps.Step1.Status"]);
        Assert.Equal("Toyota Etios 2021", projection.Fields["Steps.Step1.VehicleTitle"]);
    }

    [Fact]
    public async Task Typing_a_document_already_counts_as_reaching_step_two()
    {
        // Regression: steps used to count only once COMPLETED, so a lead that had
        // already handed over its DNI still read "paso 1" in the dashboard.
        var projection = await Project(Event(
            LeadEventNames.DocumentEntered, payload: new() { ["dni"] = "30111222" }));

        Assert.Equal(2, projection.MinLastStep);
        Assert.Equal("started", projection.Fields["Steps.Step2.Status"]);
        Assert.Equal("30111222", projection.Fields["Steps.Step2.Dni"]);
    }

    [Fact]
    public async Task Contact_details_are_lifted_to_the_root_so_recovery_can_filter_on_them()
    {
        // HU-12 only offers to recover leads that left an email; that filter reads
        // the root field, not the one nested inside step 2.
        var projection = await Project(Event(
            LeadEventNames.HolderCompleted,
            payload: new()
            {
                ["firstName"] = "Juan",
                ["lastName"] = "Perez",
                ["dni"] = "30111222",
                ["email"] = "juan@example.com",
                ["phone"] = "3511234567",
            }));

        Assert.Equal("juan@example.com", projection.Fields["ContactEmail"]);
        Assert.Equal("3511234567", projection.Fields["ContactPhone"]);
        Assert.Equal("juan@example.com", projection.Fields["Steps.Step2.Email"]);
        Assert.Equal("completed", projection.Fields["Steps.Step2.Status"]);
        Assert.Equal(2, projection.MinLastStep);
    }

    [Fact]
    public async Task A_missing_payload_value_is_recorded_as_absent_rather_than_guessed()
    {
        var projection = await Project(Event(LeadEventNames.PlateEntered));

        Assert.Null(projection.Fields["Steps.Step1.Plate"]);
    }

    // ---- Numbers ---------------------------------------------------------------

    [Fact]
    public async Task A_quoted_amount_survives_as_a_decimal()
    {
        var projection = await Project(Event(
            LeadEventNames.ProductSelected,
            payload: new() { ["productId"] = "p1", ["productName"] = "SOAT Full", ["amount"] = 28000.50d }));

        Assert.Equal(28000.50m, projection.Fields["Steps.Step3.Amount"]);
        Assert.Equal("SOAT Full", projection.Fields["Steps.Step3.ProductName"]);
    }

    [Fact]
    public async Task An_amount_arriving_as_text_is_read_with_the_invariant_culture()
    {
        // JSON numbers reach the handler as strings often enough; a comma-decimal
        // reading would turn 28000.50 into 2 800 050 on a Spanish machine.
        var projection = await Project(Event(
            LeadEventNames.ProductSelected, payload: new() { ["amount"] = "28000.50" }));

        Assert.Equal(28000.50m, projection.Fields["Steps.Step3.Amount"]);
    }

    [Fact]
    public async Task An_unparseable_amount_is_dropped_instead_of_defaulting_to_zero()
    {
        // Zero would read as a free policy in the dashboard.
        var projection = await Project(Event(
            LeadEventNames.ProductSelected, payload: new() { ["amount"] = "not a number" }));

        Assert.Null(projection.Fields["Steps.Step3.Amount"]);
    }

    [Fact]
    public async Task The_number_of_quoted_options_is_stored_as_an_integer()
    {
        var projection = await Project(Event(
            LeadEventNames.BudgetCalculated, payload: new() { ["optionsCount"] = 2 }));

        Assert.Equal(2, projection.Fields["Steps.Step3.OptionsCount"]);
        Assert.Equal("quoted", projection.Fields["Steps.Step3.Status"]);
        Assert.Equal(3, projection.MinLastStep);
    }

    // ---- Status transitions ----------------------------------------------------

    [Fact]
    public async Task A_rejected_payment_leaves_the_lead_active_so_the_visitor_can_retry()
    {
        var projection = await Project(Event(LeadEventNames.PaymentRejected));

        Assert.Null(projection.Status);
        Assert.Null(projection.MinLastStep);
        Assert.Equal("rejected", projection.Fields["Steps.Step4.Status"]);
        Assert.True(projection.RevivesLead);
    }

    [Fact]
    public async Task An_abandoned_payment_marks_the_lead_abandoned_but_never_undoes_a_purchase()
    {
        var projection = await Project(Event(LeadEventNames.PaymentAbandoned));

        Assert.Equal(LeadStatus.Abandoned, projection.Status);
        Assert.True(projection.OnlyIfNotCompleted);
        Assert.False(projection.RevivesLead);
    }

    [Fact]
    public async Task An_issued_policy_completes_the_lead_at_step_five()
    {
        var projection = await Project(Event(
            LeadEventNames.PolicyIssued, payload: new() { ["policyNumber"] = "PVV-2026-000001" }));

        Assert.Equal(LeadStatus.Completed, projection.Status);
        Assert.Equal(5, projection.MinLastStep);
        Assert.Equal("PVV-2026-000001", projection.Fields["Steps.Step5.PolicyNumber"]);
        Assert.False(projection.RevivesLead);
    }

    [Fact]
    public async Task A_failed_emission_does_not_complete_the_lead()
    {
        var projection = await Project(Event(LeadEventNames.EmissionFailed));

        Assert.Null(projection.Status);
        Assert.Equal("failed", projection.Fields["Steps.Step5.Status"]);
    }

    [Theory]
    [InlineData(LeadEventNames.PlateEntered)]
    [InlineData(LeadEventNames.DocumentEntered)]
    [InlineData(LeadEventNames.ProductSelected)]
    [InlineData(LeadEventNames.PaymentInitiated)]
    public async Task Any_sign_of_activity_brings_an_abandoned_lead_back(string eventName)
    {
        // Regression: a lead marked abandoned by the sweep stayed abandoned even
        // when the visitor came back and carried on.
        var projection = await Project(Event(eventName));

        Assert.True(projection.RevivesLead);
    }

    [Fact]
    public async Task Opening_the_portal_records_the_visit_without_entering_the_funnel()
    {
        var projection = await Project(Event(LeadEventNames.SessionStart));

        Assert.Empty(projection.Fields);
        Assert.Null(projection.MinLastStep);
    }

    [Fact]
    public async Task A_wizard_error_is_kept_on_the_lead_without_moving_it()
    {
        var projection = await Project(Event(
            LeadEventNames.WizardError, payload: new() { ["message"] = "no coverage available" }));

        Assert.Equal("no coverage available", projection.Fields["LastError"]);
        Assert.Null(projection.MinLastStep);
        Assert.Null(projection.Status);
    }

    // ---- Identity carried through ----------------------------------------------

    [Fact]
    public async Task The_projection_carries_the_tenant_and_session_of_the_event()
    {
        var projection = await Project(Event(LeadEventNames.PlateEntered));

        Assert.Equal("flow-1", projection.FlowId);
        Assert.Equal("company-token", projection.CompanyToken);
        Assert.Equal("session-1", projection.SessionId);
    }

    // ---- Helpers ---------------------------------------------------------------

    private async Task<LeadProjection> Project(LeadEvent leadEvent)
    {
        LeadProjection? captured = null;
        _leads.Setup(l => l.ApplyAsync(It.IsAny<LeadProjection>(), It.IsAny<CancellationToken>()))
            .Callback<LeadProjection, CancellationToken>((p, _) => captured = p)
            .Returns(Task.CompletedTask);

        await _service.ProjectAsync(leadEvent, CancellationToken.None);

        Assert.NotNull(captured);
        return captured;
    }

    private static LeadEvent Event(
        string name,
        string flowId = "flow-1",
        Dictionary<string, object?>? payload = null) =>
        new(name, flowId, "company-token", "session-1", payload, DateTime.UtcNow);
}
