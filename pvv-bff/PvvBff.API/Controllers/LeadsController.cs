using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PvvBff.Application.Leads.Queries;

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
    /// The company whose leads the caller may read, taken from its signed token.
    /// Null means no company owns this caller, and therefore no leads are visible.
    /// </summary>
    private string? CompanyScope()
    {
        var claim = User.FindFirst(CompanyTokenClaim)?.Value;
        return string.IsNullOrWhiteSpace(claim) ? null : claim;
    }
}
