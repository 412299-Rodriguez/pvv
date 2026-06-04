using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StackExchange.Redis;

namespace PvvBff.Infrastructure;

/// <summary>
/// Infrastructure-layer service registration (Redis + MongoDB).
/// RabbitMQ and external HTTP clients are wired up in Sprint 1.
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

        // TODO Sprint 1: RabbitMQ connection/publisher, Mongo repositories,
        //                SoatApiClient, MercadoPagoClient, SessionService.

        return services;
    }
}
