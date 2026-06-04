using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PvvConfig.Infrastructure.Persistence;
using StackExchange.Redis;

namespace PvvConfig.Infrastructure;

/// <summary>
/// Infrastructure-layer service registration (EF Core SQL Server + Redis).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core — SQL Server (database pvv_config_db).
        services.AddDbContext<ConfigDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("SqlServer")));

        // Redis — config cache + Pub/Sub for cache invalidation.
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(redisConnectionString));
        }

        // TODO Sprint 1: register repositories, ICacheInvalidator (Redis Pub/Sub),
        //                and the cache sync services.

        return services;
    }
}
