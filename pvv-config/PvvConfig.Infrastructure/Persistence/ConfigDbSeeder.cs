using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PvvConfig.Application.DTOs.ConfigurationValues;
using PvvConfig.Application.Interfaces;
using PvvConfig.Domain.Constants;
using PvvConfig.Domain.Entities;
using PvvConfig.Domain.Enums;

namespace PvvConfig.Infrastructure.Persistence;

/// <summary>
/// Seeds development data. The system admin is ensured on every startup; the demo
/// company is seeded once with a FIXED id (so its hashed portal token is stable
/// across reseeds) and is never recreated if it already exists, so companies
/// created from pvv-admin persist.
/// </summary>
public static class ConfigDbSeeder
{
    // Fixed id → with deterministic encryption, a stable `?c=` portal token forever.
    private static readonly Guid DemoCompanyId = new("11111111-1111-1111-1111-111111111111");

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

        var now = DateTime.UtcNow;

        // 1. Ensure the system administrator always exists (independent of companies).
        if (!await context.Operators.AnyAsync(o => o.Username == "superadmin@pvv.com", ct))
        {
            context.Operators.Add(new Operator
            {
                OperatorId = Guid.NewGuid(),
                CompanyId = null,
                Username = "superadmin@pvv.com",
                PasswordHash = passwordHasher.Hash("Super123!"),
                Role = OperatorRole.SystemAdmin,
                IsActive = true,
                CreatedAt = now,
            });
            await context.SaveChangesAsync(ct);
        }

        // 2. Seed the demo company once. If it already exists we stop here, so
        //    companies created from the admin (and any edits) are preserved.
        if (await context.Companies.AnyAsync(c => c.CompanyId == DemoCompanyId, ct))
        {
            return;
        }

        var company = new Company
        {
            CompanyId = DemoCompanyId,
            HashedCompanyId = encryption.Encrypt(DemoCompanyId.ToString()),
            Name = "Aseguradora Demo",
            CUIT = "30-12345678-9",
            IsActive = true,
            CreatedAt = now
        };

        var op = new Operator
        {
            OperatorId = Guid.NewGuid(),
            CompanyId = DemoCompanyId,
            Username = "admin@demo.com",
            PasswordHash = passwordHasher.Hash("Demo123!"),
            Role = OperatorRole.CompanyOperator,
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
            ],
            TermsText =
                "Estos términos definen los derechos del usuario, las restricciones de uso y las " +
                "políticas de privacidad, protegiendo tanto a la plataforma como a su cuenta. Al " +
                "aceptar estos términos estás validando lo siguiente:\n\n" +
                "• Consentimiento informado: el usuario acepta cómo se recopilan y tratan sus datos " +
                "personales (Política de Privacidad).\n" +
                "• Uso responsable: se compromete a no realizar actividades maliciosas (hackeo, spam, " +
                "suplantación de identidad).\n" +
                "• Seguridad de la cuenta: la aseguradora asume la responsabilidad de proteger los " +
                "datos del usuario y cualquier acción realizada bajo su sesión.\n" +
                "• Propiedad intelectual: el usuario reconoce los derechos de autor sobre el contenido, " +
                "diseño y código de la plataforma.",
            PrivacyText =
                "En Aseguradora Demo protegemos tus datos personales. Solo recopilamos la información " +
                "necesaria para cotizar y emitir tu póliza (patente, documento y datos de contacto), y " +
                "la tratamos de forma confidencial.\n\n" +
                "• No compartimos tus datos con terceros ajenos a la emisión del seguro.\n" +
                "• Tus datos viajan encriptados de extremo a extremo.\n" +
                "• Podés solicitar la baja o rectificación de tus datos cuando quieras.",
            Faqs =
            [
                new FaqItemDto
                {
                    Question = "¿Cómo cotizo mi seguro?",
                    Answer = "Ingresá la patente de tu vehículo y en segundos te mostramos las coberturas disponibles."
                },
                new FaqItemDto
                {
                    Question = "¿La póliza se emite al instante?",
                    Answer = "Sí, una vez confirmado el pago la póliza se emite automáticamente y la ves en pantalla."
                },
                new FaqItemDto
                {
                    Question = "¿Qué medios de pago aceptan?",
                    Answer = "Pagás de forma segura con Mercado Pago: tarjeta de crédito, débito o dinero en cuenta."
                },
                new FaqItemDto
                {
                    Question = "¿Mis datos están protegidos?",
                    Answer = "Sí, tus datos viajan encriptados y solo se usan para emitir tu póliza."
                }
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
            BuildConfiguration(DemoCompanyId, ConfigurationTypes.PVV_UI_CONFIG, uiConfig, now),
            BuildConfiguration(DemoCompanyId, ConfigurationTypes.PRODUCT_CONFIG, productConfig, now),
            BuildConfiguration(DemoCompanyId, ConfigurationTypes.PRICING_CONFIG, pricingConfig, now));

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
