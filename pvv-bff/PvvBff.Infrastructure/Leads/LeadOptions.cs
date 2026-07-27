namespace PvvBff.Infrastructure.Leads;

/// <summary>Lead tracking settings (configuration section "Leads").</summary>
public sealed class LeadOptions
{
    public const string SectionName = "Leads";

    /// <summary>
    /// Minutes without activity after which an active lead is considered abandoned.
    /// Covers visitors who leave before reaching the payment step.
    /// </summary>
    public int AbandonmentTtlMinutes { get; set; } = 30;

    /// <summary>How long raw events are kept in event_logs before the TTL index drops them.</summary>
    public int EventLogRetentionDays { get; set; } = 90;
}
