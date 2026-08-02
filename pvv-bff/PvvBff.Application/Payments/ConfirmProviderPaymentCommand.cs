using MediatR;
using Microsoft.Extensions.Logging;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Payments;

/// <summary>
/// Apply a provider notification identified only by the PROVIDER's payment id.
///
/// This is the shape a real webhook arrives in: it says "something happened to payment
/// 170770768958" and nothing else — not the amount, not the status, and not which of our
/// transactions it belongs to. All of that is read back from the provider, which is also
/// what makes the notification safe to receive: nothing in it is believed.
/// </summary>
public sealed record ConfirmProviderPaymentCommand(string ProviderPaymentId)
    : IRequest<ConfirmPaymentResult>;

public sealed class ConfirmProviderPaymentHandler
    : IRequestHandler<ConfirmProviderPaymentCommand, ConfirmPaymentResult>
{
    private readonly IPaymentGateway _gateway;
    private readonly ISender _mediator;
    private readonly ILogger<ConfirmProviderPaymentHandler> _logger;

    public ConfirmProviderPaymentHandler(
        IPaymentGateway gateway,
        ISender mediator,
        ILogger<ConfirmProviderPaymentHandler> logger)
    {
        _gateway = gateway;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ConfirmPaymentResult> Handle(
        ConfirmProviderPaymentCommand request, CancellationToken ct)
    {
        var payment = await _gateway.GetPaymentAsync(request.ProviderPaymentId, ct);
        if (payment is null)
        {
            _logger.LogWarning(
                "Notification for provider payment {ProviderPaymentId}, which the provider does not know",
                request.ProviderPaymentId);
            return new ConfirmPaymentResult(Found: false, Published: false, Status: "not_found");
        }

        if (string.IsNullOrWhiteSpace(payment.TransactionId))
        {
            // A payment created outside this portal — the provider account may be used
            // for other things. Not an error, just none of our business.
            _logger.LogInformation(
                "Provider payment {ProviderPaymentId} carries no external reference; ignoring",
                request.ProviderPaymentId);
            return new ConfirmPaymentResult(Found: false, Published: false, Status: "not_ours");
        }

        return await _mediator.Send(
            new ConfirmPaymentCommand(
                payment.TransactionId, payment.Outcome, payment.ProviderPaymentId, payment.ExpiresAt),
            ct);
    }
}
