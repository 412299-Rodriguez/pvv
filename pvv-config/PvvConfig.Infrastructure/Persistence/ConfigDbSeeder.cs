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
            LogoUrl = "https://dummyimage.com/160x48/0071ce/ffffff&text=Aseguradora",
            WelcomeText = "Cotizá tu seguro",
            FooterText = "Aseguradora Demo 2026",
            CompanyDisplayName = "Aseguradora Demo",
            Texts = new Dictionary<string, string>
            {
                ["introTitle"] = "Cotizá tu seguro en minutos",
                ["introSubtitle"] = "100% digital, sin papeles, sin filas.",
                ["feature1Title"] = "Emisión inmediata",
                ["feature1Text"] = "Tu póliza en segundos",
                ["feature2Title"] = "100% online",
                ["feature2Text"] = "Sin turnos ni papeles",
                ["feature3Title"] = "Pago seguro",
                ["feature3Text"] = "Encriptado y protegido",
                ["ratingText"] = "Miles de pólizas emitidas",
                ["plateTitle"] = "Ingresá la patente",
                ["plateSubtitle"] = "Validamos los datos al instante",
                ["plateCta"] = "Cotizar mi seguro",
                ["secureNote"] = "Tus datos están protegidos",
                ["holderTitle"] = "¿Quién es el titular?",
                ["holderSubtitle"] = "Ingresá tu documento para personalizar la cotización",
                ["coverageTitle"] = "Elegí tu cobertura"
            },
            AdImages =
            [
                "https://dummyimage.com/800x200/0071ce/ffffff&text=Promo+1",
                "https://dummyimage.com/800x200/003d7a/ffffff&text=Promo+2",
                "https://dummyimage.com/800x200/00a0e9/ffffff&text=Promo+3"
            ]
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
