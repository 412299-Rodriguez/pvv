namespace PvvBff.Domain.Leads;

/// <summary>
/// One raw wizard event, exactly as it was received. Append-only audit trail in
/// the MongoDB collection <c>event_logs</c>, expired automatically by a TTL index
/// on <see cref="OccurredAt"/>. The <see cref="Lead"/> is the queryable view;
/// this is the source it was derived from.
/// </summary>
public sealed class EventLogEntry
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Event name (e.g. "plate_validated"); see LeadEventNames.</summary>
    public string Name { get; set; } = string.Empty;

    public string? FlowId { get; set; }

    public string? CompanyToken { get; set; }

    public string? SessionId { get; set; }

    /// <summary>Free-form event payload, serialized as-is.</summary>
    public string? Payload { get; set; }

    public DateTime OccurredAt { get; set; }
}
