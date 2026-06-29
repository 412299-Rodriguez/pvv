using System.Text.Json;
using Microsoft.Extensions.Options;
using PvvBff.API.Configuration;
using StackExchange.Redis;

namespace PvvBff.API.Middleware;

/// <summary>
/// Maintains an anonymous session per visitor. Reads the session id from the
/// X-Session-Id header (cross-origin friendly) or the pvv-session cookie; creates
/// one in Redis if absent; refreshes its sliding TTL; and exposes the session id
/// and company token in <see cref="HttpContext.Items"/> for the ingress handler.
/// </summary>
public sealed class SessionMiddleware
{
    public const string SessionItemKey = "SessionId";
    public const string CompanyItemKey = "CompanyId";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly AnonymousSessionOptions _options;

    public SessionMiddleware(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        IOptions<AnonymousSessionOptions> options)
    {
        _next = next;
        _redis = redis;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IngressPath.Matches(context))
        {
            await _next(context);
            return;
        }

        var companyToken = context.Request.Headers["X-Company-Token"].ToString();
        var sessionId = context.Request.Headers["X-Session-Id"].ToString();
        if (string.IsNullOrWhiteSpace(sessionId))
            sessionId = context.Request.Cookies[_options.CookieName] ?? string.Empty;

        var db = _redis.GetDatabase();
        var ttl = TimeSpan.FromMinutes(_options.TtlMinutes);

        SessionData? data = null;
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var existing = await db.StringGetAsync($"session:{sessionId}");
            if (!existing.IsNullOrEmpty)
                data = JsonSerializer.Deserialize<SessionData>(existing.ToString(), JsonOptions);
        }

        if (data is null)
        {
            sessionId = Guid.NewGuid().ToString("N");
            data = new SessionData(
                sessionId,
                string.IsNullOrWhiteSpace(companyToken) ? null : companyToken,
                context.Items[FingerprintMiddleware.ItemKey] as string,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow);
        }
        else
        {
            data = data with { LastSeenAt = DateTimeOffset.UtcNow };
        }

        // Sliding expiry: re-write with a fresh TTL on every request.
        await db.StringSetAsync($"session:{sessionId}", JsonSerializer.Serialize(data, JsonOptions), ttl);

        context.Items[SessionItemKey] = sessionId;
        if (!string.IsNullOrWhiteSpace(companyToken))
            context.Items[CompanyItemKey] = companyToken;

        context.Response.Cookies.Append(_options.CookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            MaxAge = ttl,
        });

        await _next(context);
    }

    private sealed record SessionData(
        string Id,
        string? CompanyToken,
        string? Fingerprint,
        DateTimeOffset CreatedAt,
        DateTimeOffset LastSeenAt);
}
