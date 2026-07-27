namespace PvvBff.Application.Leads;

/// <summary>
/// One tracked wizard event. Some are reported by the front through the
/// LEAD_EVENT ingress hash; the payment and emission ones are raised in-process
/// by the BFF itself, because they are server-side facts the browser cannot
/// observe (it has left the site by then).
/// </summary>
/// <param name="Name">One of <see cref="LeadEventNames"/>.</param>
/// <param name="FlowId">The purchase attempt this event belongs to.</param>
/// <param name="Payload">Event data, looked up case-insensitively.</param>
public sealed record LeadEvent(
    string Name,
    string FlowId,
    string? CompanyToken,
    string? SessionId,
    IReadOnlyDictionary<string, object?>? Payload,
    DateTime OccurredAt);

/// <summary>
/// The tracked event vocabulary (docs/arquitectura-pvv.md §6.5). Anything else is
/// rejected by the ingress handler so a typo in the front cannot pollute the funnel.
/// </summary>
public static class LeadEventNames
{
    // ---- Reported by the front ------------------------------------------------
    public const string SessionStart = "session_start";
    public const string PlateEntered = "plate_entered";
    public const string PlateValidated = "plate_validated";
    public const string DocumentEntered = "document_entered";
    public const string HolderCompleted = "holder_completed";
    public const string BudgetCalculated = "budget_calculated";
    public const string ProductSelected = "product_selected";
    public const string WizardError = "wizard_error";

    // ---- Raised server-side by the BFF ---------------------------------------
    public const string PaymentInitiated = "payment_initiated";
    public const string PaymentConfirmed = "payment_confirmed";
    public const string PaymentRejected = "payment_rejected";
    public const string PaymentAbandoned = "payment_abandoned";
    public const string PolicyIssued = "policy_issued";
    public const string EmissionFailed = "emission_failed";

    /// <summary>Events the front is allowed to report.</summary>
    public static readonly IReadOnlySet<string> FrontendEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SessionStart,
        PlateEntered,
        PlateValidated,
        DocumentEntered,
        HolderCompleted,
        BudgetCalculated,
        ProductSelected,
        WizardError,
    };
}
