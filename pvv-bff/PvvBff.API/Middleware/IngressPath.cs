namespace PvvBff.API.Middleware;

/// <summary>
/// The custom security/session middlewares only apply to ingress traffic, so
/// /health and Swagger stay open.
/// </summary>
internal static class IngressPath
{
    public static bool Matches(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/api/ingress", StringComparison.OrdinalIgnoreCase);
}
