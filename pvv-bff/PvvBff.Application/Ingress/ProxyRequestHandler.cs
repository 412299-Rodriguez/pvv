using MediatR;
using Microsoft.Extensions.Logging;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Ingress;

/// <summary>
/// Resolves the route for a hash, then dispatches: <c>internal://</c> routes go
/// to the matching in-process handler (looked up by key, no switch); everything
/// else is proxied transparently to the internal service.
/// </summary>
public sealed class ProxyRequestHandler : IRequestHandler<ProxyRequestCommand, IngressResponse>
{
    private readonly IRouteStore _routes;
    private readonly IInternalHttpProxy _proxy;
    private readonly IReadOnlyDictionary<string, IInternalIngressHandler> _internalHandlers;
    private readonly ILogger<ProxyRequestHandler> _logger;

    public ProxyRequestHandler(
        IRouteStore routes,
        IInternalHttpProxy proxy,
        IEnumerable<IInternalIngressHandler> internalHandlers,
        ILogger<ProxyRequestHandler> logger)
    {
        _routes = routes;
        _proxy = proxy;
        _internalHandlers = internalHandlers.ToDictionary(h => h.Key, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    public async Task<IngressResponse> Handle(ProxyRequestCommand request, CancellationToken ct)
    {
        var route = await _routes.FindAsync(request.Hash, ct);
        if (route is null)
        {
            _logger.LogWarning("Ingress: unknown route hash {Hash}", request.Hash);
            return IngressResponse.Failure(404, $"Unknown route '{request.Hash}'.");
        }

        var context = new IngressExecutionContext(
            route, request.Body, request.CompanyId, request.SessionId, request.CorrelationId);

        if (route.IsInternal)
        {
            if (!_internalHandlers.TryGetValue(route.InternalKey, out var handler))
            {
                _logger.LogError("Ingress: no internal handler registered for key '{Key}'", route.InternalKey);
                return IngressResponse.Failure(500, $"No internal handler for '{route.InternalKey}'.");
            }

            _logger.LogInformation("Ingress: {Hash} → internal handler '{Key}'", request.Hash, route.InternalKey);
            return await handler.HandleAsync(context, ct);
        }

        _logger.LogInformation("Ingress: {Hash} → proxy {Method} {Endpoint}",
            request.Hash, route.Method, route.Endpoint);
        return await _proxy.SendAsync(context, ct);
    }
}
