using PvvBff.Application.Leads.Queries;

namespace PvvBff.Application.Abstractions;

/// <summary>
/// Read side of the leads module, kept apart from <see cref="ILeadStore"/>: the
/// writer applies one event at a time, this one runs aggregations for the admin
/// dashboard. Returns DTOs, never the domain document.
/// </summary>
public interface ILeadQueryStore
{
    Task<LeadFunnelDto> GetFunnelAsync(LeadQueryFilter filter, CancellationToken ct);

    Task<PagedLeadsDto> GetLeadsAsync(LeadQueryFilter filter, int page, int pageSize, CancellationToken ct);
}
