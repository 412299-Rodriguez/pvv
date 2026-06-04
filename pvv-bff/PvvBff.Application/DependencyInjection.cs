using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace PvvBff.Application;

/// <summary>
/// Application-layer service registration (MediatR).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR scans this assembly for IRequestHandler implementations
        // (ingress handlers, lead-event handlers, etc.).
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
