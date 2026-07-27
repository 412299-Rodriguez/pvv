using MediatR;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Leads.Queries;

/// <summary>Conversion funnel and headline numbers for the admin dashboard.</summary>
public sealed record GetLeadFunnelQuery(LeadQueryFilter Filter) : IRequest<LeadFunnelDto>;

/// <summary>One rung of the funnel.</summary>
/// <param name="Step">Milestone number, 1-5.</param>
/// <param name="Reached">Leads that got at least this far.</param>
/// <param name="ConversionFromPrevious">
/// Share of the previous rung that made it here, 0-100. For step 1 it is measured
/// against everyone who opened the portal.
/// </param>
public sealed record LeadFunnelStepDto(
    int Step,
    string Label,
    long Reached,
    decimal ConversionFromPrevious);

/// <param name="OverallConversion">Portal visits that ended in a policy, 0-100.</param>
public sealed record LeadFunnelDto(
    long TotalLeads,
    long Active,
    long Abandoned,
    long Completed,
    long PoliciesIssued,
    decimal OverallConversion,
    IReadOnlyList<LeadFunnelStepDto> Steps);

public sealed class GetLeadFunnelHandler : IRequestHandler<GetLeadFunnelQuery, LeadFunnelDto>
{
    private readonly ILeadQueryStore _store;

    public GetLeadFunnelHandler(ILeadQueryStore store) => _store = store;

    public Task<LeadFunnelDto> Handle(GetLeadFunnelQuery request, CancellationToken ct) =>
        _store.GetFunnelAsync(request.Filter, ct);
}
