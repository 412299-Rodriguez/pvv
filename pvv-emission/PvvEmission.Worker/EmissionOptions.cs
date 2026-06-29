namespace PvvEmission.Worker;

/// <summary>Emission retry policy, bound from "Emission".</summary>
public sealed class EmissionOptions
{
    public const string SectionName = "Emission";

    /// <summary>
    /// Delay before each retry of a transient failure, in seconds. The number of
    /// entries is the number of retries; once exhausted the message is dead-lettered.
    /// Empty here on purpose (the config binder appends to a non-empty default);
    /// the worker falls back to <see cref="DefaultRetryDelaysSeconds"/> when unset.
    /// </summary>
    public int[] RetryDelaysSeconds { get; set; } = [];

    /// <summary>The 1→2→3 min backoff used when no delays are configured.</summary>
    public static readonly int[] DefaultRetryDelaysSeconds = [60, 120, 180];
}
