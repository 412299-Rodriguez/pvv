using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PvvConfig.Application.Interfaces;
using PvvConfig.Infrastructure.Persistence;
using PvvConfig.Infrastructure.Repositories;
using PvvConfig.Infrastructure.Services;
using StackExchange.Redis;

namespace PvvConfig.Infrastructure;

/// <summary>
/// Infrastructure-layer service registration (EF Core, Redis, repositories,
/// services and unit of work).
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

        // Repositories + unit of work.
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IOperatorRepository, OperatorRepository>();
        services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        services.AddScoped<IUnitOfWork, ConfigUnitOfWork>();

        // Stateless services.
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        services.AddSingleton<IConfigCache, RedisConfigCache>();

        return services;
    }
}
