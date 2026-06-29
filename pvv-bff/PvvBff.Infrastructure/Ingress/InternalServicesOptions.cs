namespace PvvBff.Infrastructure.Ingress;

/// <summary>
/// Configuration for talking to internal services (bound from "Services").
/// </summary>
public sealed class InternalServicesOptions
{
    public const string SectionName = "Services";

    /// <summary>Base URL of pvv-config, used as the config-read HTTP fallback.</summary>
    public string ConfigBaseUrl { get; set; } = "http://localhost:5002";

    /// <summary>Base URL of pvv-soat, used by the typed soat gateway.</summary>
    public string SoatBaseUrl { get; set; } = "http://localhost:5001";

    /// <summary>Ingress routes seed file, relative to the content root.</summary>
    public string RoutesFile { get; set; } = "ingress-routes.json";
}
