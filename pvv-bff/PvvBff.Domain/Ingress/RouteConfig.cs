using System.Text.Json.Serialization;

namespace PvvBff.Domain.Ingress;

/// <summary>
/// A dynamic ingress route: maps an opaque hash sent by the frontend to an
/// internal destination. A full-URL <see cref="Endpoint"/> is proxied
/// transparently to that service; an <c>internal://{key}</c> endpoint is
/// dispatched in-process to the BFF handler registered under that key.
/// </summary>
public sealed class RouteConfig
{
    /// <summary>Opaque hash the frontend sends (e.g. "PLATE_SEARCH").</summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>HTTP method used against the internal service (proxy routes).</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Full URL (proxy) or "internal://{key}" (in-process handler).</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>Proxy call timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 15;

    /// <summary>Informational tag of the target service (e.g. "pvv-soat").</summary>
    public string? TargetService { get; set; }

    private const string InternalPrefix = "internal://";

    /// <summary>True when the route runs an in-process handler instead of a proxy.</summary>
    [JsonIgnore]
    public bool IsInternal => Endpoint.StartsWith(InternalPrefix, StringComparison.OrdinalIgnoreCase);

    /// <summary>The handler key for an internal route (e.g. "company-config").</summary>
    [JsonIgnore]
    public string InternalKey => IsInternal ? Endpoint[InternalPrefix.Length..] : string.Empty;
}
