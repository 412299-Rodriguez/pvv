using System.Security.Cryptography;
using System.Text;

namespace PvvBff.API.Middleware;

/// <summary>
/// Computes a per-visitor fingerprint — SHA-256 of IP + User-Agent + client id —
/// and stashes it in <see cref="HttpContext.Items"/> for the session middleware
/// and downstream use. Ingress traffic only.
/// </summary>
public sealed class FingerprintMiddleware
{
    public const string ItemKey = "Fingerprint";

    private readonly RequestDelegate _next;

    public FingerprintMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (IngressPath.Matches(context))
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var clientId = context.Request.Headers["X-Client-Id"].ToString();

            var raw = $"{ip}|{userAgent}|{clientId}";
            context.Items[ItemKey] = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        }

        await _next(context);
    }
}
