using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Ingress;

namespace PvvBff.Infrastructure.Ingress;

/// <summary>
/// Transparent HTTP proxy to internal services. Substitutes <c>{placeholder}</c>
/// segments in the endpoint from the request body, forwards the body for write
/// methods, attaches the internal trust headers, and enforces a per-route timeout.
/// </summary>
public sealed partial class HttpInternalProxy : IInternalHttpProxy
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IHttpClientFactory _httpClientFactory;

    public HttpInternalProxy(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    public async Task<IngressResponse> SendAsync(IngressExecutionContext context, CancellationToken ct)
    {
        var route = context.Route;
        var method = new HttpMethod(route.Method.ToUpperInvariant());
        var url = BuildUrl(route.Endpoint, context.Body);

        using var message = new HttpRequestMessage(method, url);

        if (context.Body is { } body && method != HttpMethod.Get && method != HttpMethod.Delete)
        {
            message.Content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
        }

        message.Headers.TryAddWithoutValidation("X-BFF-Internal", "true");
        if (!string.IsNullOrEmpty(context.CompanyId))
            message.Headers.TryAddWithoutValidation("X-Company-Id", context.CompanyId);
        if (!string.IsNullOrEmpty(context.SessionId))
            message.Headers.TryAddWithoutValidation("X-Session-Id", context.SessionId);
        message.Headers.TryAddWithoutValidation("X-Correlation-Id", context.CorrelationId);

        var timeout = route.TimeoutSeconds <= 0 ? 15 : route.TimeoutSeconds;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeout));

        try
        {
            using var response = await _httpClientFactory.CreateClient("internal")
                .SendAsync(message, timeoutCts.Token);
            var content = await response.Content.ReadAsStringAsync(timeoutCts.Token);

            return response.IsSuccessStatusCode
                ? IngressResponse.Success((int)response.StatusCode, ParseContent(content))
                : IngressResponse.Failure(
                    (int)response.StatusCode,
                    string.IsNullOrWhiteSpace(content) ? response.ReasonPhrase ?? "Upstream error" : content);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return IngressResponse.Failure(504, $"Upstream timeout after {timeout}s.");
        }
        catch (HttpRequestException ex)
        {
            return IngressResponse.Failure(502, $"Upstream unreachable: {ex.Message}");
        }
    }

    private static object? ParseContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(content, JsonOptions);
        }
        catch (JsonException)
        {
            return content;
        }
    }

    private static string BuildUrl(string endpoint, JsonElement? body)
    {
        if (!endpoint.Contains('{'))
            return endpoint;

        return PlaceholderRegex().Replace(endpoint, match =>
        {
            var key = match.Groups[1].Value;
            if (body is { ValueKind: JsonValueKind.Object } obj && obj.TryGetProperty(key, out var value))
                return Uri.EscapeDataString(value.ToString());
            return match.Value;
        });
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex PlaceholderRegex();
}
