using Microsoft.Extensions.Options;
using PvvBff.Application.Abstractions;

namespace PvvBff.Infrastructure.Payments;

/// <summary>
/// Mock payment gateway. Returns a fake preference id and an init_point that
/// points to our own mock-checkout page (carrying the transaction id).
///
/// Kept alive after HU-11 on purpose: with "Payments:Gateway" set to Mock the whole
/// purchase flow runs with no Mercado Pago credentials and no internet, which is
/// what makes the demo portable.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    private readonly PaymentOptions _options;

    public MockPaymentGateway(IOptions<PaymentOptions> options) => _options = options.Value;

    public Task<PaymentPreferenceResult> CreatePreferenceAsync(
        PaymentPreferenceRequest request, CancellationToken ct)
    {
        var preferenceId = $"mock-{Guid.NewGuid():N}";
        var initPoint = $"{_options.MockCheckoutBaseUrl}?tx={request.TransactionId}&pref={preferenceId}";
        return Task.FromResult(new PaymentPreferenceResult(preferenceId, initPoint));
    }

    // There is no provider to interrogate: the mock checkout confirms by calling
    // our own webhook directly, so nothing is ever left to reconcile. Returning
    // null keeps every caller working unchanged instead of forcing them to know
    // which gateway they are talking to.
    public Task<GatewayPayment?> GetPaymentAsync(string providerPaymentId, CancellationToken ct) =>
        Task.FromResult<GatewayPayment?>(null);

    public Task<GatewayPayment?> FindPaymentByTransactionAsync(string transactionId, CancellationToken ct) =>
        Task.FromResult<GatewayPayment?>(null);
}
