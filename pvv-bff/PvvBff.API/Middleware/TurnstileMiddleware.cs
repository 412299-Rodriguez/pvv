using Microsoft.Extensions.Options;
using PvvBff.API.Configuration;

namespace PvvBff.API.Middleware;

/// <summary>
/// Verifies the Cloudflare Turnstile token (header X-Turnstile-Token) against
/// Cloudflare's siteverify endpoint before letting an ingress request through.
/// Locally this runs against Cloudflare's always-pass test secret.
/// </summary>
public sealed class TurnstileMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TurnstileOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TurnstileMiddleware> _logger;

    public TurnstileMiddleware(
        RequestDelegate next,
        IOptions<TurnstileOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<TurnstileMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IngressPath.Matches(context) || !_options.Enabled)
        {
            await _next(context);
            return;
        }

        var token = context.Request.Headers["X-Turnstile-Token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            await RejectAsync(context, "Missing Turnstile token.");
            return;
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        if (!await IsValidAsync(token, remoteIp, context.RequestAborted))
        {
            await RejectAsync(context, "Turnstile verification failed.");
            return;
        }

        await _next(context);
    }

    private async Task<bool> IsValidAsync(string token, string? remoteIp, CancellationToken ct)
    {
        try
        {
            var form = new Dictionary<string, string>
            {
                ["secret"] = _options.SecretKey,
                ["response"] = token,
            };
            if (!string.IsNullOrEmpty(remoteIp))
                form["remoteip"] = remoteIp;

            using var response = await _httpClientFactory.CreateClient("internal")
                .PostAsync(_options.VerifyUrl, new FormUrlEncodedContent(form), ct);
            var result = await response.Content.ReadFromJsonAsync<TurnstileResult>(ct);
            return result?.Success ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Turnstile verification request failed");
            return false;
        }
    }

    private static async Task RejectAsync(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { statusCode = 403, data = (object?)null, error = detail });
    }

    private sealed record TurnstileResult(bool Success);
}
