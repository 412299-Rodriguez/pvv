using Microsoft.Extensions.Options;
using PvvBff.Application.Abstractions;

namespace PvvBff.Infrastructure.Payments;

/// <summary>
/// Mock payment gateway. Returns a fake preference id and an init_point that
/// points to our own mock-checkout page (carrying the transaction id). HU-11
/// replaces this with the real Mercado Pago preference API — same interface.
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
}
