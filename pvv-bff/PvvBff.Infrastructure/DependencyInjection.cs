using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using PvvBff.Application.Abstractions;
using PvvBff.Infrastructure.Ingress;
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

        // MongoDB — leads and event logs (database pvv_bff_db).
        var mongoConnectionString = configuration.GetConnectionString("MongoDB");
        if (!string.IsNullOrWhiteSpace(mongoConnectionString))
        {
            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
            services.AddSingleton(sp =>
                sp.GetRequiredService<IMongoClient>().GetDatabase("pvv_bff_db"));
        }

        // Ingress (HU-06) — route store, in-process proxy, company-config reader,
        // and the startup seeder that loads ingress-routes.json into Redis.
        services.Configure<InternalServicesOptions>(
            configuration.GetSection(InternalServicesOptions.SectionName));
        services.AddHttpClient("internal");
        services.AddScoped<IRouteStore, RedisRouteStore>();
        services.AddScoped<IInternalHttpProxy, HttpInternalProxy>();
        services.AddScoped<ICompanyConfigReader, RedisCompanyConfigReader>();
        services.AddHostedService<IngressRoutesSeederHostedService>();

        // TODO HU-08: RabbitMQ publisher, MercadoPago gateway.
        // TODO HU-07: Mongo lead repositories.

        return services;
    }
}
