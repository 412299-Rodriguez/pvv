using PvvBff.Domain.Ingress;

namespace PvvBff.Application.Abstractions;

/// <summary>Reads ingress routes (seeded into Redis) by their hash.</summary>
public interface IRouteStore
{
    Task<RouteConfig?> FindAsync(string hash, CancellationToken ct);
}
