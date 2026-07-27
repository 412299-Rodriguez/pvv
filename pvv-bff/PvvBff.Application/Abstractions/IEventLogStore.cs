using PvvBff.Domain.Leads;

namespace PvvBff.Application.Abstractions;

/// <summary>Append-only log of raw wizard events (expired by a TTL index).</summary>
public interface IEventLogStore
{
    Task AppendAsync(EventLogEntry entry, CancellationToken ct);
}
