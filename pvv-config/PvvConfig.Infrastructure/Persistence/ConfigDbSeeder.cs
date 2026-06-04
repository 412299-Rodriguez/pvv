using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PvvConfig.Application.DTOs.ConfigurationValues;
using PvvConfig.Application.Interfaces;
using PvvConfig.Domain.Constants;
using PvvConfig.Domain.Entities;

namespace PvvConfig.Infrastructure.Persistence;

/// <summary>
/// Seeds example data for local development only. Runs when the environment is
/// Development and the Companies table is empty.
/// </summary>
public static class ConfigDbSeeder
{
    public static async Task SeedAsync(
        ConfigDbContext context,
        IEncryptionService encryption,
        IPasswordHasher passwordHasher,
        IHostEnvironment environment,
        CancellationToken ct = default)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        if (await context.Companies.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var companyId = Guid.NewGuid();

        var company = new Company
        {
            CompanyId = companyId,
            HashedCompanyId = encryption.Encrypt(companyId.ToString()),
            Name = "Aseguradora Demo",
            CUIT = "30-12345678-9",
            IsActive = true,
            CreatedAt = now
        };

        var op = new Operator
        {
            OperatorId = Guid.NewGuid(),
            CompanyId = companyId,
            Username = "admin@demo.com",
            PasswordHash = passwordHasher.Hash("Demo123!"),
            IsActive = true,
            CreatedAt = now
        };

        var uiConfig = new UiConfigValueDto
        {
            PrimaryColor = "#0071ce",
            SecondaryColor = "#003d7a",
            LogoUrl = "",
            WelcomeText = "Cotizá tu seguro",
            FooterText = "Aseguradora Demo 2026",
            CompanyDisplayName = "Demo"
        };

        var product1 = Guid.NewGuid();
        var product2 = Guid.NewGuid();

        var productConfig = new ProductConfigValueDto
        {
            Products =
            [
                new ProductItemDto
                {
                    ProductId = product1,
                    Name = "SOAT Básico",
                    CoverageType = "Responsabilidad Civil",
                    Conditions = "Cobertura mínima obligatoria",
                    IsActive = true
                },
                new ProductItemDto
                {
                    ProductId = product2,
                    Name = "SOAT Full",
                    CoverageType = "Todo Riesgo",
                    Conditions = "Cobertura ampliada con franquicia",
                    IsActive = true
                }
            ]
        };

        var pricingConfig = new PricingConfigValueDto
        {
            Rules =
            [
                new PricingItemDto
                {
                    PricingId = Guid.NewGuid(),
                    ProductId = product1,
                    VehicleType = "Car",
                    YearFrom = 2010,
                    YearTo = 2026,
                    Price = 15000m
                },
                new PricingItemDto
                {
                    PricingId = Guid.NewGuid(),
                    ProductId = product2,
                    VehicleType = "Car",
                    YearFrom = 2010,
                    YearTo = 2026,
                    Price = 28000m
                }
            ]
        };

        context.Companies.Add(company);
        context.Operators.Add(op);
        context.Configurations.AddRange(
            BuildConfiguration(companyId, ConfigurationTypes.PVV_UI_CONFIG, uiConfig, now),
            BuildConfiguration(companyId, ConfigurationTypes.PRODUCT_CONFIG, productConfig, now),
            BuildConfiguration(companyId, ConfigurationTypes.PRICING_CONFIG, pricingConfig, now));

        await context.SaveChangesAsync(ct);
    }

    private static Configuration BuildConfiguration(
        Guid companyId, string type, object value, DateTime now) => new()
    {
        ConfigurationId = Guid.NewGuid(),
        CompanyId = companyId,
        ConfigurationType = type,
        ConfigurationValue = JsonSerializer.Serialize(value),
        IsActive = true,
        Version = 1,
        CreatedAt = now,
        UpdatedAt = now
    };
}
