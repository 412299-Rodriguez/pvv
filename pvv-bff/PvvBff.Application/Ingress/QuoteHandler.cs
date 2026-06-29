using System.Text.Json;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Ingress;

/// <summary>
/// Internal ingress handler for <c>internal://quote</c> (hash QUOTE). Builds the
/// coverage options from the company's PRODUCT_CONFIG + PRICING_CONFIG (pvv-config),
/// applying the pricing rule that matches the vehicle's type and year. This is
/// what makes the products/prices fully customizable per company.
/// </summary>
public sealed class QuoteHandler : IInternalIngressHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ISoatGateway _soat;
    private readonly ICompanyConfigReader _config;

    public QuoteHandler(ISoatGateway soat, ICompanyConfigReader config)
    {
        _soat = soat;
        _config = config;
    }

    public string Key => "quote";

    public async Task<IngressResponse> HandleAsync(IngressExecutionContext context, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(context.CompanyId))
            return IngressResponse.Failure(400, "Falta el token de compañía.");

        var plate = ReadString(context.Body, "plate");
        if (string.IsNullOrWhiteSpace(plate))
            return IngressResponse.Failure(400, "Falta la patente.");

        var vehicle = await _soat.GetVehicleByPlateAsync(plate, ct);
        if (vehicle is null)
            return IngressResponse.Failure(404, "No encontramos ese vehículo.");

        var products = await ReadConfigAsync<ProductConfig>(context.CompanyId, "PRODUCT_CONFIG", ct);
        var pricing = await ReadConfigAsync<PricingConfig>(context.CompanyId, "PRICING_CONFIG", ct);
        if (products is null || pricing is null)
            return IngressResponse.Failure(404, "No hay productos configurados para la compañía.");

        var coverages = new List<Coverage>();
        foreach (var product in products.Products.Where(p => p.IsActive))
        {
            var rule = pricing.Rules.FirstOrDefault(r =>
                r.ProductId == product.ProductId &&
                string.Equals(r.VehicleType, vehicle.VehicleType, StringComparison.OrdinalIgnoreCase) &&
                vehicle.Year >= r.YearFrom && vehicle.Year <= r.YearTo);
            if (rule is null)
                continue;

            coverages.Add(new Coverage(
                Id: product.ProductId,
                Name: product.Name,
                CoverageType: product.CoverageType,
                PricePerYear: rule.Price,
                Recommended: false,
                Benefits: string.IsNullOrWhiteSpace(product.Conditions) ? [] : [product.Conditions]));
        }

        // Flag the most expensive option as the recommended ("Más elegido") one.
        if (coverages.Count > 0)
        {
            var top = coverages.MaxBy(c => c.PricePerYear)!;
            var index = coverages.IndexOf(top);
            coverages[index] = top with { Recommended = true };
        }

        return IngressResponse.Success(200, new { coverages });
    }

    private async Task<T?> ReadConfigAsync<T>(string companyHash, string type, CancellationToken ct)
    {
        var raw = await _config.GetByHashAsync(companyHash, type, ct);
        return raw switch
        {
            null => default,
            JsonElement element => element.Deserialize<T>(JsonOptions),
            _ => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(raw), JsonOptions),
        };
    }

    private static string ReadString(JsonElement? body, string key)
    {
        if (body is { ValueKind: JsonValueKind.Object } obj &&
            obj.TryGetProperty(key, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private sealed record Coverage(
        string Id,
        string Name,
        string CoverageType,
        decimal PricePerYear,
        bool Recommended,
        string[] Benefits);

    private sealed record ProductConfig(List<ProductItem> Products);

    private sealed record ProductItem(string ProductId, string Name, string CoverageType, string Conditions, bool IsActive);

    private sealed record PricingConfig(List<PricingRule> Rules);

    private sealed record PricingRule(string ProductId, string VehicleType, int YearFrom, int YearTo, decimal Price);
}
