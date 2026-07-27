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
/// </summary>
[ApiController]
[Route("api/leads")]
[Authorize]
public sealed class LeadsController : ControllerBase
{
    private const string SystemAdminRole = "SystemAdmin";
    private const string CompanyTokenClaim = "companyToken";

    private readonly ISender _mediator;

    public LeadsController(ISender mediator) => _mediator = mediator;

    /// <summary>Conversion funnel and headline numbers.</summary>
    [HttpGet("funnel")]
    public async Task<IActionResult> GetFunnel(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? companyToken,
        CancellationToken ct)
    {
        if (!TryResolveCompanyToken(companyToken, out var scope))
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
        [FromQuery] string? companyToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!TryResolveCompanyToken(companyToken, out var scope))
            return Forbid();

        var filter = new LeadQueryFilter(scope, from, to, lastStep, status);
        return Ok(await _mediator.Send(new GetLeadsQuery(filter, page, pageSize), ct));
    }

    /// <summary>
    /// Decides which company the caller may read. A company operator is pinned to
    /// the token inside its own JWT — the query string cannot widen that. Only a
    /// SystemAdmin may name a company, or omit it to span all of them.
    /// </summary>
    private bool TryResolveCompanyToken(string? requested, out string? scope)
    {
        var claim = User.FindFirst(CompanyTokenClaim)?.Value;
        if (!string.IsNullOrWhiteSpace(claim))
        {
            scope = claim;
            return true;
        }

        if (User.IsInRole(SystemAdminRole))
        {
            scope = requested;
            return true;
        }

        scope = null;
        return false;
    }
}
