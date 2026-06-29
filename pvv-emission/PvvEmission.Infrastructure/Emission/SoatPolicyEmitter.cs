using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PvvEmission.Application.Emission;

namespace PvvEmission.Infrastructure.Emission;

/// <summary>
/// Emits policies by calling pvv-soat (POST /api/policies/emit). Maps soat's HTTP
/// outcome to an <see cref="EmitOutcome"/> the worker uses to decide ack/retry/DLQ.
/// </summary>
public sealed class SoatPolicyEmitter : IPolicyEmitter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ILogger<SoatPolicyEmitter> _logger;

    public SoatPolicyEmitter(HttpClient http, ILogger<SoatPolicyEmitter> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<EmitOutcome> EmitAsync(string budgetId, CancellationToken ct)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(
                "/api/policies/emit", new { budgetId }, JsonOptions, ct);

            if (response.IsSuccessStatusCode)
            {
                var policy = await response.Content.ReadFromJsonAsync<SoatPolicyResponse>(JsonOptions, ct);
                return EmitOutcome.Success(policy?.PolicyNumber ?? "unknown");
            }

            return response.StatusCode switch
            {
                HttpStatusCode.Conflict => EmitOutcome.AlreadyEmitted(),
                HttpStatusCode.BadRequest or HttpStatusCode.NotFound =>
                    EmitOutcome.InvalidData($"soat returned {(int)response.StatusCode}"),
                _ => EmitOutcome.Transient($"soat returned {(int)response.StatusCode}"),
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "soat emit call failed (transient) for budget {BudgetId}", budgetId);
            return EmitOutcome.Transient(ex.Message);
        }
    }

    private sealed record SoatPolicyResponse(string PolicyNumber);
}
