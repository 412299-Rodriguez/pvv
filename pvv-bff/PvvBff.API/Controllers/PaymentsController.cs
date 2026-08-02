using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvBff.API.Models;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Payments;

namespace PvvBff.API.Controllers;

/// <summary>
/// Payment provider callbacks. This is a SERVER-TO-SERVER channel, not the frontend's
/// ingress: these are invoked by Mercado Pago (or, for the mock gateway, by our own
/// mock-checkout page), so they carry no Turnstile token and no session, and they sit
/// outside /api/ingress on purpose.
///
/// Two endpoints rather than one, because the two payloads have nothing in common: the
/// mock reports our transaction id and an outcome, while Mercado Pago reports its own
/// payment id and signs the request. Merging them would mean sniffing the shape of the
/// body, and would put the mock behind a signature it cannot produce.
/// </summary>
[ApiController]
[Route("api/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _mediator;
    private readonly IPaymentWebhookVerifier _verifier;

    public PaymentsController(ISender mediator, IPaymentWebhookVerifier verifier)
    {
        _mediator = mediator;
        _verifier = verifier;
    }

    /// <summary>
    /// Confirmation webhook for the MOCK gateway — called by our own mock-checkout page.
    /// On the first approval it confirms the transaction and publishes the emission job.
    /// Idempotent.
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

    /// <summary>
    /// Real Mercado Pago notification. Verifies the signature, then reads the payment
    /// back from their API and applies it.
    ///
    /// Answers 2xx for anything it can make a decision about — including notifications
    /// that turn out not to concern us. Mercado Pago retries on any non-2xx for hours,
    /// so a status code here is a statement about whether it should try again, not about
    /// whether we liked the message.
    /// </summary>
    [HttpPost("mp/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> MercadoPagoWebhook(
        [FromBody] MercadoPagoNotification? notification, CancellationToken ct)
    {
        // Mercado Pago sends the same facts twice, in the query string and in the body.
        // Its signature is computed over the query's data.id, so that is the one that
        // has to be used — falling back to the body only when the query is absent.
        var type = Request.Query["type"].FirstOrDefault() ?? notification?.Type;
        var paymentId = Request.Query["data.id"].FirstOrDefault() ?? notification?.Data?.Id;

        // merchant_order and other topics land on this same URL.
        if (!string.Equals(type, "payment", StringComparison.OrdinalIgnoreCase))
            return Ok(new { ignored = true, reason = "not_a_payment_topic", type });

        if (string.IsNullOrWhiteSpace(paymentId))
            return Ok(new { ignored = true, reason = "no_payment_id" });

        // Fail closed. Accepting unverified notifications because the secret happens to
        // be missing is the kind of misconfiguration that should be loud, not convenient.
        if (!_verifier.IsConfigured)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { error = "Webhook signature secret is not configured." });
        }

        if (!_verifier.Verify(
                paymentId,
                Request.Headers["x-signature"].ToString(),
                Request.Headers["x-request-id"].ToString()))
        {
            return Unauthorized(new { error = "Invalid signature." });
        }

        var result = await _mediator.Send(new ConfirmProviderPaymentCommand(paymentId), ct);

        return Ok(new { paymentId, status = result.Status, published = result.Published });
    }
}
