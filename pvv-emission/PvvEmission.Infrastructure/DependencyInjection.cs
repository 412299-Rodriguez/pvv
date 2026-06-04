using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RabbitMQ.Client;

namespace PvvEmission.Infrastructure;

/// <summary>
/// Infrastructure-layer service registration (MongoDB + RabbitMQ connection factory).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // MongoDB — emission status tracking (database pvv_bff_db, shared with the BFF).
        var mongoConnectionString = configuration.GetConnectionString("MongoDB");
        if (!string.IsNullOrWhiteSpace(mongoConnectionString))
        {
            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
            services.AddSingleton(sp =>
                sp.GetRequiredService<IMongoClient>().GetDatabase("pvv_bff_db"));
        }

        // RabbitMQ — connection factory for the emission queue consumer.
        // The connection itself is opened by the consumer in Sprint 1.
        var rabbit = configuration.GetSection("RabbitMQ");
        services.AddSingleton(_ => new ConnectionFactory
        {
            HostName = rabbit["Host"] ?? "localhost",
            VirtualHost = rabbit["VHost"] ?? "pvv",
            UserName = rabbit["User"] ?? "pvv_user",
            Password = rabbit["Pass"] ?? "pvv_pass"
        });

        // TODO Sprint 1: RabbitMQ consumer wiring, SoatApiClient, emission repositories.

        return services;
    }
}
