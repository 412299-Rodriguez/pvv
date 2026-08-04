using MediatR;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Leads.Queries;

/// <summary>One page of the leads table, newest first.</summary>
public sealed record GetLeadsQuery(LeadQueryFilter Filter, int Page, int PageSize)
    : IRequest<PagedLeadsDto>;

/// <summary>A lead as the admin table shows it — flattened out of the step tree.</summary>
public sealed record LeadListItemDto(
    string FlowId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int LastStep,
    string Status,
    string? Plate,
    string? VehicleTitle,
    string? HolderName,
    string? Dni,
    string? Email,
    string? Phone,
    string? ProductName,
    decimal? Amount,
    string? PaymentStatus,
    string? PolicyNumber,
    /// <summary>
    /// When the recovery email went out, if it did. The table needs it to stop offering
    /// an action that would be refused, and to show that somebody already reached out.
    /// </summary>
    DateTime? RecoveredAt = null,
    string? RecoveredBy = null);

public sealed record PagedLeadsDto(
    IReadOnlyList<LeadListItemDto> Items,
    long Total,
    int Page,
    int PageSize);

public sealed class GetLeadsHandler : IRequestHandler<GetLeadsQuery, PagedLeadsDto>
{
    private const int MaxPageSize = 200;

    private readonly ILeadQueryStore _store;

    public GetLeadsHandler(ILeadQueryStore store) => _store = store;

    public Task<PagedLeadsDto> Handle(GetLeadsQuery request, CancellationToken ct)
    {
        // Clamp rather than reject: a bad page size is not worth an error page.
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > MaxPageSize ? 20 : request.PageSize;

        return _store.GetLeadsAsync(request.Filter, page, pageSize, ct);
    }
}
