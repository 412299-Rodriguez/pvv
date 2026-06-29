namespace PvvBff.API.Configuration;

/// <summary>
/// Anonymous-session settings, bound from "Session". Named to avoid clashing
/// with ASP.NET's built-in <c>SessionOptions</c>.
/// </summary>
public sealed class AnonymousSessionOptions
{
    public const string SectionName = "Session";

    public string CookieName { get; set; } = "pvv-session";

    /// <summary>Sliding lifetime of the session in Redis (and the cookie).</summary>
    public int TtlMinutes { get; set; } = 30;
}
