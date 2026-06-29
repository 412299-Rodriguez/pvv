using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PvvBff.Application.Ingress;
using PvvBff.Application.Payments;

namespace PvvBff.Application;

/// <summary>
/// Application-layer service registration (MediatR + internal ingress handlers).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR scans this assembly for IRequestHandler implementations
        // (ingress handlers, lead-event handlers, etc.).
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Internal ingress handlers (internal://{key}) — resolved by key in a
        // DI-built registry. Add one line per new concern; no central switch.
        services.AddScoped<IInternalIngressHandler, CompanyConfigHandler>();
        services.AddScoped<IInternalIngressHandler, PaymentInitHandler>();
        services.AddScoped<IInternalIngressHandler, PlateSearchHandler>();

        return services;
    }
}
