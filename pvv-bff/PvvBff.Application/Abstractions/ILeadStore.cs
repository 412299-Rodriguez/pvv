using PvvBff.Application.Leads;

namespace PvvBff.Application.Abstractions;

/// <summary>
/// Persistence for the lead funnel view. The Application layer decides WHAT
/// changes (a <see cref="LeadProjection"/>); the implementation knows how to
/// write it — so no Mongo update definitions leak upwards.
/// </summary>
public interface ILeadStore
{
    /// <summary>Applies one projection, creating the lead if it does not exist yet.</summary>
    Task ApplyAsync(LeadProjection projection, CancellationToken ct);

    /// <summary>
    /// Marks every still-active lead whose last activity predates
    /// <paramref name="inactiveBefore"/> as abandoned. Returns how many were marked.
    /// </summary>
    Task<long> MarkStaleAsAbandonedAsync(DateTime inactiveBefore, CancellationToken ct);
}
