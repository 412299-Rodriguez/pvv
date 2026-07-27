using System.Text.Json;
using PvvBff.Application.Ingress;

namespace PvvBff.Application.Leads;

/// <summary>
/// Internal ingress handler for <c>internal://lead-event</c> (hash LEAD_EVENT).
/// The wizard reports one event per user action here; the payment and emission
/// events are NOT accepted from the front — the BFF raises those itself.
/// </summary>
public sealed class LeadEventHandler : IInternalIngressHandler
{
    private readonly ILeadProjectionService _projection;

    public LeadEventHandler(ILeadProjectionService projection) => _projection = projection;

    public string Key => "lead-event";

    public async Task<IngressResponse> HandleAsync(IngressExecutionContext context, CancellationToken ct)
    {
        if (context.Body is not { ValueKind: JsonValueKind.Object } body)
            return IngressResponse.Failure(400, "LEAD_EVENT requires an object body.");

        var name = ReadString(body, "event");
        var flowId = ReadString(body, "flowId");

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(flowId))
            return IngressResponse.Failure(400, "LEAD_EVENT requires 'event' and 'flowId'.");

        // Server-side facts (payment, emission) can only be raised in-process.
        if (!LeadEventNames.FrontendEvents.Contains(name))
            return IngressResponse.Failure(400, $"Event '{name}' is not reportable by the client.");

        await _projection.ProjectAsync(
            new LeadEvent(
                Name: name,
                FlowId: flowId,
                CompanyToken: context.CompanyId,
                SessionId: context.SessionId,
                Payload: ReadPayload(body),
                // Server clock: the browser's is neither trusted nor comparable.
                OccurredAt: DateTime.UtcNow),
            ct);

        return IngressResponse.Success(200, new { accepted = true });
    }

    private static string ReadString(JsonElement body, string key) =>
        body.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    /// <summary>Flattens the payload object into primitives the projection can read.</summary>
    private static IReadOnlyDictionary<string, object?>? ReadPayload(JsonElement body)
    {
        if (!body.TryGetProperty("payload", out var payload) || payload.ValueKind != JsonValueKind.Object)
            return null;

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in payload.EnumerateObject())
        {
            result[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.TryGetDecimal(out var number) ? number : null,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null,
            };
        }

        return result;
    }
}
