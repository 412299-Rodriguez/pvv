namespace PvvBff.Application.Abstractions;

/// <summary>Input for creating a checkout preference.</summary>
/// <param name="CompanyToken">
/// The portal the purchase belongs to, carried into the return URL. The provider
/// performs that redirect itself, so unlike the mock the frontend has no chance to
/// append it — and without it a buyer whose browser storage was cleared would come
/// back to a portal that cannot identify its tenant.
/// </param>
public sealed record PaymentPreferenceRequest(
    string TransactionId,
    decimal Amount,
    string Description,
    string? CompanyToken);

/// <summary>The provider's preference id and the URL the user is sent to.</summary>
public sealed record PaymentPreferenceResult(string PreferenceId, string InitPoint);

/// <summary>
/// What the provider says about a payment, reduced to the three outcomes our state
/// machine can act on.
/// </summary>
public enum PaymentOutcome
{
    /// <summary>
    /// Still in flight — decide nothing. NOT a synonym for failure: Mercado Pago
    /// reports pending/in_process for cash coupons, bank transfers and fraud
    /// review, and every one of those may still be approved hours later.
    /// </summary>
    Pending,

    Approved,

    Rejected,
}

/// <summary>A payment as the provider reports it.</summary>
/// <param name="ProviderPaymentId">The provider's own id for the payment.</param>
/// <param name="TransactionId">OUR transaction id, carried as the provider's external reference.</param>
/// <param name="Outcome">The provider's status mapped onto <see cref="PaymentOutcome"/>.</param>
/// <param name="RawStatus">The provider's own status string, kept for logs.</param>
public sealed record GatewayPayment(
    string ProviderPaymentId,
    string TransactionId,
    PaymentOutcome Outcome,
    string RawStatus);

/// <summary>
/// Abstraction over the payment provider. Two implementations exist and both are
/// live: a mock that redirects to our own checkout page (so the purchase flow can
/// be demonstrated with no credentials and no internet) and the real Mercado Pago
/// one. "Payments:Gateway" decides which is registered.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentPreferenceResult> CreatePreferenceAsync(PaymentPreferenceRequest request, CancellationToken ct);

    /// <summary>
    /// Reads a payment by the PROVIDER's id — which is all a webhook notification
    /// carries. Null when the provider does not know it.
    /// </summary>
    Task<GatewayPayment?> GetPaymentAsync(string providerPaymentId, CancellationToken ct);

    /// <summary>
    /// Finds the payment made against one of OUR transactions.
    ///
    /// This is the pull half of the confirmation, and it is what makes the sale
    /// independent of a notification ever arriving: we can ask instead of waiting.
    /// Null when the provider has no payment for it yet.
    /// </summary>
    Task<GatewayPayment?> FindPaymentByTransactionAsync(string transactionId, CancellationToken ct);
}
