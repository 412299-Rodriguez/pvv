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

    /// <summary>The company the portal belongs to (HashedCompanyId).</summary>
    public string? CompanyToken { get; set; }

    /// <summary>The soat budget this payment is for (its id, as a string).</summary>
    public string BudgetId { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    /// <summary>Provider preference id (mock id in HU-08, real MP id in HU-11).</summary>
    public string? PreferenceId { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; }

    public DateTime? ConfirmedAt { get; set; }
}
