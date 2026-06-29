namespace PvvBff.API.Configuration;

/// <summary>Fixed-window rate-limit settings, bound from "RateLimit".</summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int PermitLimit { get; set; } = 60;

    public int WindowSeconds { get; set; } = 60;
}
