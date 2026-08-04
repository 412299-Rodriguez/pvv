using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvBff.Application.Leads.Queries;
using PvvBff.Application.Leads.Recovery;

namespace PvvBff.API.Controllers;

/// <summary>
/// Read-only funnel data for pvv-admin. Unlike the wizard, the caller here is an
/// authenticated operator, so these endpoints sit outside <c>/api/ingress</c> and
/// its anonymous pipeline (Turnstile, anonymous session, rate limiting) and are
/// guarded by the JWT that pvv-config issues instead.
///
/// Leads belong to the company that produced them. Access comes from the signed
/// companyToken claim and nothing else — there is no way to ask for another
/// company's leads, and the platform administrator (who carries no such claim)
/// has no reach here either.
/// </summary>
[ApiController]
[Route("api/leads")]
[Authorize]
public sealed class LeadsController : ControllerBase
{
    private const string CompanyTokenClaim = "companyToken";

    private readonly ISender _mediator;

    public LeadsController(ISender mediator) => _mediator = mediator;

    /// <summary>Conversion funnel and headline numbers.</summary>
    [HttpGet("funnel")]
    public async Task<IActionResult> GetFunnel(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var scope = CompanyScope();
        if (scope is null)
            return Forbid();

        var filter = new LeadQueryFilter(scope, from, to);
        return Ok(await _mediator.Send(new GetLeadFunnelQuery(filter), ct));
    }

    /// <summary>One page of the leads table, newest first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLeads(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? lastStep,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var scope = CompanyScope();
        if (scope is null)
            return Forbid();

        var filter = new LeadQueryFilter(scope, from, to, lastStep, status);
        return Ok(await _mediator.Send(new GetLeadsQuery(filter, page, pageSize), ct));
    }

    /// <summary>
    /// Sends the recovery email for an abandoned lead (HU-12).
    ///
    /// A POST and not a GET because it has an effect in the world that cannot be taken
    /// back: somebody receives an email. The outcomes are distinguished on purpose — an
    /// operator deserves to know whether the lead has no address, was already written
    /// to, or the provider refused the message, because each one calls for something
    /// different.
    /// </summary>
    [HttpPost("{flowId}/recover")]
    public async Task<IActionResult> Recover(string flowId, CancellationToken ct)
    {
        var scope = CompanyScope();
        if (scope is null)
            return Forbid();

        var result = await _mediator.Send(
            new RecoverLeadCommand(flowId, scope, OperatorName()), ct);

        return result.Status switch
        {
            "sent" => Ok(new { status = result.Status }),
            "not_found" => NotFound(new { status = result.Status, error = "Lead inexistente." }),
            "no_contact" => Conflict(new
            {
                status = result.Status,
                error = "Este lead no dejó una dirección de correo.",
            }),
            "already_recovered" => Conflict(new
            {
                status = result.Status,
                error = "A este lead ya se le envió el correo de recupero.",
            }),
            // The provider refused it. A 502 rather than a 500: nothing here failed,
            // the service we depend on did, and the operator can try again later.
            _ => StatusCode(StatusCodes.Status502BadGateway, new
            {
                status = result.Status,
                error = result.Error ?? "No se pudo enviar el correo.",
            }),
        };
    }

    /// <summary>
    /// Who is sending, for the record kept on the lead. Falls back rather than failing:
    /// the identity is already proven by the token, this is only a label.
    /// </summary>
    private string OperatorName() =>
        User.FindFirst("username")?.Value
        ?? User.Identity?.Name
        ?? "operador";

    /// <summary>
    /// The company whose leads the caller may read, taken from its signed token.
    /// Null means no company owns this caller, and therefore no leads are visible.
    /// </summary>
    private string? CompanyScope()
    {
        var claim = User.FindFirst(CompanyTokenClaim)?.Value;
        return string.IsNullOrWhiteSpace(claim) ? null : claim;
    }
}
