using System.Text.Json;
using PvvBff.Domain.Ingress;

namespace PvvBff.Application.Ingress;

/// <summary>
/// Everything the proxy or an internal handler needs to serve one ingress call:
/// the resolved route, the request body, and the caller's identity/correlation.
/// </summary>
public sealed record IngressExecutionContext(
    RouteConfig Route,
    JsonElement? Body,
    string? CompanyId,
    string? SessionId,
    string CorrelationId);
