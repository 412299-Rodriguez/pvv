using PvvBff.Domain.Leads;

namespace PvvBff.Application.Abstractions;

/// <summary>
/// The reads and writes lead recovery needs. Separate from <see cref="ILeadQueryStore"/>
/// on purpose: that one serves the dashboard and only ever reads.
/// </summary>
public interface ILeadRecoveryStore
{
    /// <summary>
    /// One lead, but only if it belongs to <paramref name="companyToken"/>. The scope is
    /// part of the query rather than a check afterwards, so there is no path that reads
    /// another tenant's lead and then decides what to do about it.
    /// </summary>
    Task<Lead?> GetScopedAsync(string flowId, string companyToken, CancellationToken ct);

    /// <summary>
    /// Claims the right to send this lead's recovery email, and says whether the caller
    /// won it. The claim IS the write — it only matches a lead that has not been
    /// recovered yet — so two operators pressing the button at the same moment cannot
    /// both send. Checking a flag and then setting it would let them.
    /// </summary>
    Task<bool> TryClaimRecoveryAsync(string flowId, DateTime at, string by, CancellationToken ct);

    /// <summary>
    /// Gives the claim back after a send that never left. Without this a provider
    /// outage would permanently mark the lead as contacted by an email nobody received,
    /// which is the worst of both outcomes: no message and no second chance.
    /// </summary>
    Task ReleaseRecoveryClaimAsync(string flowId, CancellationToken ct);
}
