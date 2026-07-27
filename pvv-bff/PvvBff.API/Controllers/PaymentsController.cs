using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvBff.API.Models;
using PvvBff.Application.Abstractions;
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
    /// Confirmation webhook for the MOCK gateway — called by our own mock-checkout
    /// page. On the first approval it confirms the transaction and publishes the
    /// emission job. Idempotent.
    ///
    /// The real Mercado Pago notification does NOT land here: its payload carries a
    /// provider payment id rather than ours and it is signed, so it gets its own
    /// endpoint. Keeping the two apart is what lets the mock keep working.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] PaymentWebhookRequest request, CancellationToken ct)
    {
        // The mock only ever reports one of two things, so anything that is not an
        // approval is a rejection. A real provider also has in-flight states, which
        // is why PaymentOutcome has three cases and not two.
        var outcome = string.Equals(request.Status, "approved", StringComparison.OrdinalIgnoreCase)
            ? PaymentOutcome.Approved
            : PaymentOutcome.Rejected;

        var result = await _mediator.Send(
            new ConfirmPaymentCommand(request.TransactionId, outcome), ct);

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
