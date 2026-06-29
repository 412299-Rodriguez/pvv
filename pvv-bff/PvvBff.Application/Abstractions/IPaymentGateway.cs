namespace PvvBff.Application.Abstractions;

/// <summary>Input for creating a checkout preference.</summary>
public sealed record PaymentPreferenceRequest(string TransactionId, decimal Amount, string Description);

/// <summary>The provider's preference id and the URL the user is sent to.</summary>
public sealed record PaymentPreferenceResult(string PreferenceId, string InitPoint);

/// <summary>
/// Abstraction over the payment provider. HU-08 ships a MOCK implementation that
/// points the init_point at our own mock-checkout page; HU-11 swaps it for the
/// real Mercado Pago preference API without changing any caller.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentPreferenceResult> CreatePreferenceAsync(PaymentPreferenceRequest request, CancellationToken ct);
}
