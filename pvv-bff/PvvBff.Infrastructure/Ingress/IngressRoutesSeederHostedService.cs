using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PvvBff.Domain.Ingress;
using StackExchange.Redis;

namespace PvvBff.Infrastructure.Ingress;

/// <summary>
/// Seeds the ingress route table into Redis at startup from ingress-routes.json
/// (key <c>route:{hash}</c>). Routes can then be changed without a redeploy by
/// editing Redis directly.
/// </summary>
public sealed class IngressRoutesSeederHostedService : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IConnectionMultiplexer _redis;
    private readonly InternalServicesOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<IngressRoutesSeederHostedService> _logger;

    public IngressRoutesSeederHostedService(
        IConnectionMultiplexer redis,
        IOptions<InternalServicesOptions> options,
        IHostEnvironment environment,
        ILogger<IngressRoutesSeederHostedService> logger)
    {
        _redis = redis;
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var path = Path.Combine(_environment.ContentRootPath, _options.RoutesFile);
        if (!File.Exists(path))
        {
            _logger.LogWarning("Ingress routes file not found at {Path}; nothing seeded", path);
            return;
        }

        var json = await File.ReadAllTextAsync(path, ct);
        var file = JsonSerializer.Deserialize<RoutesFile>(json, JsonOptions);
        if (file?.Routes is not { Count: > 0 })
        {
            _logger.LogWarning("Ingress routes file {Path} contained no routes", path);
            return;
        }

        var db = _redis.GetDatabase();
        foreach (var route in file.Routes)
        {
            await db.StringSetAsync($"route:{route.Hash}", JsonSerializer.Serialize(route, JsonOptions));
        }

        _logger.LogInformation("Seeded {Count} ingress routes into Redis", file.Routes.Count);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;

    private sealed record RoutesFile(List<RouteConfig> Routes);
}
