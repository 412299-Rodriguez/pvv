using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PvvSoat.Application.Interfaces;
using PvvSoat.Infrastructure.Persistence;
using PvvSoat.Infrastructure.Repositories;

namespace PvvSoat.Infrastructure;

/// <summary>
/// Infrastructure-layer service registration (EF Core SQL Server, repositories,
/// unit of work).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SoatDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("SqlServer")));

        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IHolderRepository, HolderRepository>();
        services.AddScoped<IBudgetRepository, BudgetRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();
        services.AddScoped<IUnitOfWork, SoatUnitOfWork>();

        return services;
    }
}
