namespace PvvBff.API.Middleware;

/// <summary>
/// The custom security/session middlewares only apply to ingress traffic, so
/// /health and Swagger stay open.
/// </summary>
internal static class IngressPath
{
    public static bool Matches(HttpContext context) =>
        // Skip CORS preflight (OPTIONS) so the CORS middleware can answer it
        // instead of Turnstile rejecting a token-less preflight.
        !HttpMethods.IsOptions(context.Request.Method) &&
        context.Request.Path.StartsWithSegments("/api/ingress", StringComparison.OrdinalIgnoreCase);
}
