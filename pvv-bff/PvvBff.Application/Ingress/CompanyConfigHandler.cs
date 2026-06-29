using System.Text.Json;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Ingress;

/// <summary>
/// Internal ingress handler for <c>internal://company-config</c> (hash CONFIG_LOAD).
/// Returns the requesting company's configuration blob, resolved Redis-first with
/// an HTTP fallback to pvv-config. The company is identified by the X-Company-Token
/// header (its HashedCompanyId); the desired section comes in the body ("type").
/// </summary>
public sealed class CompanyConfigHandler : IInternalIngressHandler
{
    private readonly ICompanyConfigReader _reader;

    public CompanyConfigHandler(ICompanyConfigReader reader) => _reader = reader;

    public string Key => "company-config";

    public async Task<IngressResponse> HandleAsync(IngressExecutionContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.CompanyId))
            return IngressResponse.Failure(400, "Missing company token (X-Company-Token).");

        var type = ReadConfigurationType(context.Body);
        var config = await _reader.GetByHashAsync(context.CompanyId, type, ct);

        return config is null
            ? IngressResponse.Failure(404, $"Configuration '{type}' not found for the company.")
            : IngressResponse.Success(200, config);
    }

    private static string ReadConfigurationType(JsonElement? body)
    {
        if (body is { ValueKind: JsonValueKind.Object } obj &&
            obj.TryGetProperty("type", out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString()!;
        }

        return "PVV_UI_CONFIG"; // default section for the portal appearance
    }
}
