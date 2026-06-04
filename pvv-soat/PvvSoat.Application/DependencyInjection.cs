using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace PvvSoat.Application;

/// <summary>
/// Application-layer service registration. MediatR scans this assembly and
/// auto-registers every Command/Query handler.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
