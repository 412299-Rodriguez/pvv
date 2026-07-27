namespace PvvBff.Domain.Payments;

/// <summary>
/// A payment attempt for a budget. Created "pending" when the user starts the
/// checkout, flipped to "confirmed" by the payment webhook (which then triggers
/// emission), or "abandoned" by the background job if it is never confirmed.
/// Persisted in MongoDB (collection payment_transactions).
/// </summary>
public sealed class PaymentTransaction
{
    /// <summary>Transaction id (also the Mongo _id); echoed back in the back_url.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Anonymous session that started the checkout.</summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// The wizard flow (lead) this payment belongs to. It is the bridge back to
    /// the funnel: the browser is gone by the time the webhook and the emission
    /// land, so the lead is resolved from here instead.
    /// </summary>
    public string? FlowId { get; set; }

    /// <summary>The company the portal belongs to (HashedCompanyId).</summary>
    public string? CompanyToken { get; set; }

    /// <summary>The soat budget this payment is for (its id, as a string).</summary>
    public string BudgetId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    /// <summary>Provider preference id (mock id in HU-08, real MP id in HU-11).</summary>
    public string? PreferenceId { get; set; }

    /// <summary>
    /// The provider's own id for the payment, once one exists. Set even while the
    /// payment is still pending, which is how we know the difference between a
    /// visitor who walked away from the checkout and one who is holding a cash
    /// coupon they have not paid yet.
    /// </summary>
    public string? ProviderPaymentId { get; set; }

    /// <summary>
    /// Deadline the buyer has to complete a pending payment (a cash coupon lives for
    /// weeks). While this is in the future the transaction is NOT abandoned, however
    /// old it is.
    /// </summary>
    public DateTime? PaymentPendingUntil { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    // ---- Denormalized ticket fields (set at PAYMENT_INIT so the result page can
    // render the ticket after the redirect, without re-fetching from soat) -------
    public string? VehicleTitle { get; set; }

    public string? HolderName { get; set; }

    // ---- Emission outcome (written by pvv-emission's worker) --------------------
    public string? EmissionStatus { get; set; }

    public string? PolicyNumber { get; set; }

    public int? EmissionAttempts { get; set; }

    public DateTime? EmissionUpdatedAt { get; set; }

    /// <summary>
    /// When the emission outcome was projected onto the lead. The result page
    /// polls the status, so this keeps the funnel step from being recorded once
    /// per poll.
    /// </summary>
    public DateTime? EmissionProjectedAt { get; set; }
}
