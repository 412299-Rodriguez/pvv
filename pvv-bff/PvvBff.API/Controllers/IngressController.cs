using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvBff.API.Models;
using PvvBff.Application.Ingress;

namespace PvvBff.API.Controllers;

/// <summary>
/// The single entry point for the frontend. Every wizard call arrives here as a
/// hash; the ingress resolves it and either proxies it or runs its handler.
/// </summary>
[ApiController]
[Route("api/ingress")]
public sealed class IngressController : ControllerBase
{
    private readonly ISender _mediator;

    public IngressController(ISender mediator) => _mediator = mediator;

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Proxy([FromBody] IngressHttpRequest request, CancellationToken ct)
    {
        // Until the session middleware (6B) lands, read identity straight from the
        // headers the frontend sends.
        var companyToken = Request.Headers["X-Company-Token"].FirstOrDefault();
        var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault();

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
