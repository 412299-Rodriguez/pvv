using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace PvvConfig.Application;

/// <summary>
/// Application-layer service registration (MediatR + FluentValidation).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR scans this assembly for IRequestHandler implementations.
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // FluentValidation scans this assembly for AbstractValidator implementations.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // TODO Sprint 1: registrar handlers de Commands y Queries
        //                (se autodescubren arriba), pipeline behaviors y validators.

        return services;
    }
}
