using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PvvBff.Domain.Leads;

namespace PvvBff.Infrastructure.Leads;

/// <summary>
/// Creates the MongoDB indexes the lead module needs at startup: the TTL index
/// that expires raw events, and the lookups the admin dashboard will filter by.
/// Creating an existing, identical index is a no-op, so this runs on every boot.
/// </summary>
public sealed class MongoIndexInitializer : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly LeadOptions _options;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(
        IMongoDatabase database,
        IOptions<LeadOptions> options,
        ILogger<MongoIndexInitializer> logger)
    {
        _database = database;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            var retentionDays = _options.EventLogRetentionDays <= 0 ? 90 : _options.EventLogRetentionDays;

            var events = _database.GetCollection<EventLogEntry>(MongoEventLogStore.CollectionName);
            await events.Indexes.CreateOneAsync(
                new CreateIndexModel<EventLogEntry>(
                    Builders<EventLogEntry>.IndexKeys.Ascending(e => e.OccurredAt),
                    new CreateIndexOptions { ExpireAfter = TimeSpan.FromDays(retentionDays), Name = "event_logs_ttl" }),
                cancellationToken: ct);

            await events.Indexes.CreateOneAsync(
                new CreateIndexModel<EventLogEntry>(
                    Builders<EventLogEntry>.IndexKeys.Ascending(e => e.FlowId)),
                cancellationToken: ct);

            // Dashboard filters: by company, then by recency / funnel position.
            var leads = _database.GetCollection<Lead>(MongoLeadStore.CollectionName);
            await leads.Indexes.CreateManyAsync(
                [
                    new CreateIndexModel<Lead>(Builders<Lead>.IndexKeys
                        .Ascending(l => l.CompanyToken).Descending(l => l.CreatedAt)),
                    new CreateIndexModel<Lead>(Builders<Lead>.IndexKeys
                        .Ascending(l => l.CompanyToken).Ascending(l => l.Status)),
                    new CreateIndexModel<Lead>(Builders<Lead>.IndexKeys
                        .Ascending(l => l.CompanyToken).Ascending(l => l.LastStep)),
                    // Used by the abandonment sweep.
                    new CreateIndexModel<Lead>(Builders<Lead>.IndexKeys
                        .Ascending(l => l.Status).Ascending(l => l.UpdatedAt)),
                ],
                ct);

            _logger.LogInformation("Lead MongoDB indexes ensured (event log retention {Days} days)", retentionDays);
        }
        catch (MongoCommandException ex)
        {
            // An index that already exists with different options must be dropped
            // by hand; never block startup over it.
            _logger.LogWarning(ex, "Could not ensure lead MongoDB indexes");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
