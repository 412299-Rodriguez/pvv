using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using PvvBff.Application.Abstractions;
using PvvBff.Infrastructure.Ingress;
using PvvBff.Infrastructure.Leads;
using PvvBff.Infrastructure.Messaging;
using PvvBff.Infrastructure.Payments;
using StackExchange.Redis;

namespace PvvBff.Infrastructure;

/// <summary>
/// Infrastructure-layer service registration (Redis + MongoDB + ingress).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Redis — sessions, ingress routes and config cache (eager connect).
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(redisConnectionString));
        }

        // MongoDB — leads, event logs and payment transactions (database pvv_bff_db).
        var mongoConnectionString = configuration.GetConnectionString("MongoDB");
        if (!string.IsNullOrWhiteSpace(mongoConnectionString))
        {
            // Store decimals (money) as Decimal128 rather than the driver default.
            BsonSerializer.TryRegisterSerializer(new DecimalSerializer(BsonType.Decimal128));

            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
            services.AddSingleton(sp =>
                sp.GetRequiredService<IMongoClient>().GetDatabase("pvv_bff_db"));
            services.AddScoped<IPaymentRepository, MongoPaymentRepository>();
            services.AddHostedService<AbandonmentDetectionJob>();

            // Leads (HU-07) — funnel view + raw event log.
            services.Configure<LeadOptions>(configuration.GetSection(LeadOptions.SectionName));
            services.AddScoped<ILeadStore, MongoLeadStore>();
            services.AddScoped<ILeadQueryStore, MongoLeadQueryStore>();
            services.AddScoped<IEventLogStore, MongoEventLogStore>();
            services.AddHostedService<MongoIndexInitializer>();
        }

        // Ingress (HU-06) — route store, in-process proxy, company-config reader,
        // and the startup seeder that loads ingress-routes.json into Redis.
        services.Configure<InternalServicesOptions>(
            configuration.GetSection(InternalServicesOptions.SectionName));
        services.AddHttpClient("internal");
        var soatBaseUrl = configuration["Services:SoatBaseUrl"] ?? "http://localhost:5001";
        services.AddHttpClient<ISoatGateway, Soat.SoatGateway>(client =>
            client.BaseAddress = new Uri(soatBaseUrl));
        services.AddScoped<IRouteStore, RedisRouteStore>();
        services.AddScoped<IInternalHttpProxy, HttpInternalProxy>();
        services.AddScoped<ICompanyConfigReader, RedisCompanyConfigReader>();
        services.AddHostedService<IngressRoutesSeederHostedService>();

        // Payments — one gateway or the other, chosen by "Payments:Gateway".
        // The mock is not dead code: it keeps the purchase flow demonstrable with
        // no Mercado Pago credentials and no internet.
        services.Configure<PaymentOptions>(configuration.GetSection(PaymentOptions.SectionName));
        services.Configure<MercadoPagoOptions>(configuration.GetSection(MercadoPagoOptions.SectionName));

        var gatewayKind = configuration.GetValue<PaymentGatewayKind>(
            $"{PaymentOptions.SectionName}:Gateway");

        if (gatewayKind == PaymentGatewayKind.MercadoPago)
        {
            var mercadoPagoBaseUrl = configuration[$"{MercadoPagoOptions.SectionName}:BaseUrl"]
                ?? "https://api.mercadopago.com";
            services.AddHttpClient<IPaymentGateway, MercadoPagoGateway>(client =>
            {
                client.BaseAddress = new Uri(mercadoPagoBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(20);
            });
        }
        else
        {
            services.AddSingleton<IPaymentGateway, MockPaymentGateway>();
        }

        // Messaging (HU-08/8B) — RabbitMQ publisher for emission jobs.
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IEmissionPublisher, RabbitMqEmissionPublisher>();

        return services;
    }
}
