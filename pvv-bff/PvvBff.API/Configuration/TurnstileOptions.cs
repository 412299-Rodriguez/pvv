namespace PvvBff.API.Configuration;

/// <summary>Cloudflare Turnstile (anti-bot) settings, bound from "Turnstile".</summary>
public sealed class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    /// <summary>Server-side secret. Use Cloudflare's test key locally.</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>When false the middleware is a no-op (e.g. for diagnostics).</summary>
    public bool Enabled { get; set; } = true;

    public string VerifyUrl { get; set; } = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
}
