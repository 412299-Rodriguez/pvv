using MongoDB.Driver;
using PvvBff.Application.Abstractions;
using PvvBff.Domain.Leads;

namespace PvvBff.Infrastructure.Leads;

/// <summary>
/// Append-only event log in MongoDB (collection <c>event_logs</c>). Documents are
/// dropped automatically by the TTL index created in <see cref="MongoIndexInitializer"/>.
/// </summary>
public sealed class MongoEventLogStore : IEventLogStore
{
    public const string CollectionName = "event_logs";

    private readonly IMongoCollection<EventLogEntry> _collection;

    public MongoEventLogStore(IMongoDatabase database)
    {
        _collection = database.GetCollection<EventLogEntry>(CollectionName);
    }

    public Task AppendAsync(EventLogEntry entry, CancellationToken ct) =>
        _collection.InsertOneAsync(entry, cancellationToken: ct);
}
