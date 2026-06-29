using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvBff.API.Models;
using PvvBff.Application.Payments;

namespace PvvBff.API.Controllers;

/// <summary>
/// Payment provider callbacks. This is a SERVER-TO-SERVER channel (not the
/// frontend's ingress): the webhook is invoked by Mercado Pago — or, in HU-08,
/// by the mock-checkout page standing in for it — so it carries no Turnstile
/// token or session and is intentionally outside /api/ingress.
/// </summary>
[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _mediator;

    public PaymentsController(ISender mediator) => _mediator = mediator;

    /// <summary>
    /// Payment confirmation webhook. On the first "approved" notification it
    /// confirms the transaction and publishes the emission job. Idempotent.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] PaymentWebhookRequest request, CancellationToken ct)
    {
        // TODO HU-11: validate the real Mercado Pago webhook signature here.
        var result = await _mediator.Send(
            new ConfirmPaymentCommand(request.TransactionId, request.Status), ct);

        if (!result.Found)
            return NotFound(new { transactionId = request.TransactionId, error = "Unknown transaction." });

        return Ok(new
        {
            transactionId = request.TransactionId,
            status = result.Status,
            published = result.Published,
        });
    }
}
