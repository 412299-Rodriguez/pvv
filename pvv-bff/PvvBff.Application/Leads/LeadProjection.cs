namespace PvvBff.Application.Leads;

/// <summary>
/// The change one event makes to a lead, expressed without any storage detail:
/// a set of dot-paths to write, an optional new funnel milestone and an optional
/// new lead status. The store turns this into an atomic upsert.
/// </summary>
/// <param name="FlowId">Identifies the lead (its document id).</param>
/// <param name="Fields">Dot-path → value pairs to write (e.g. "Steps.Step1.Plate").</param>
/// <param name="MinLastStep">Raises LastStep to this value; it never decreases.</param>
/// <param name="Status">New lead-level status, when the event changes it.</param>
/// <param name="OnlyIfNotCompleted">
/// Guards the transition: skip the write when the lead is already completed (a
/// stale abandonment sweep must not undo a finished purchase).
/// </param>
public sealed record LeadProjection(
    string FlowId,
    string? CompanyToken,
    string? SessionId,
    IReadOnlyDictionary<string, object?> Fields,
    int? MinLastStep = null,
    string? Status = null,
    bool OnlyIfNotCompleted = false);
