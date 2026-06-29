using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PvvBff.API.Middleware;
using PvvBff.API.Models;
using PvvBff.Application.Ingress;

namespace PvvBff.API.Controllers;

/// <summary>
/// The single entry point for the frontend. Every wizard call arrives here as a
/// hash; the ingress resolves it and either proxies it or runs its handler.
/// </summary>
[ApiController]
[Route("api/ingress")]
[EnableRateLimiting("ingress")]
public sealed class IngressController : ControllerBase
{
    private readonly ISender _mediator;

    public IngressController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Proxy([FromBody] IngressHttpRequest request, CancellationToken ct)
    {
        // Identity is resolved by the session middleware and stashed in Items.
        var companyToken = HttpContext.Items[SessionMiddleware.CompanyItemKey] as string;
        var sessionId = HttpContext.Items[SessionMiddleware.SessionItemKey] as string;

        var result = await _mediator.Send(
            new ProxyRequestCommand(
                Hash: request.Hash,
                Body: request.Body,
                CompanyId: companyToken,
                SessionId: sessionId,
                CorrelationId: HttpContext.TraceIdentifier),
            ct);

        return StatusCode(result.StatusCode, result);
    }
}
