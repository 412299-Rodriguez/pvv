using MongoDB.Bson;
using MongoDB.Driver;
using PvvBff.Application.Abstractions;
using PvvBff.Application.Leads;
using PvvBff.Domain.Leads;

namespace PvvBff.Infrastructure.Leads;

/// <summary>
/// Stores the funnel view in MongoDB (collection <c>leads</c>) as one atomic
/// upsert per event: <c>$set</c> with dot-notation for the step fields,
/// <c>$setOnInsert</c> for the values that only apply on creation, and
/// <c>$max</c> for LastStep so a late or duplicated event can never walk the
/// funnel backwards.
/// </summary>
public sealed class MongoLeadStore : ILeadStore
{
    public const string CollectionName = "leads";

    private readonly IMongoCollection<Lead> _collection;

    public MongoLeadStore(IMongoDatabase database)
    {
        _collection = database.GetCollection<Lead>(CollectionName);
    }

    public async Task ApplyAsync(LeadProjection projection, CancellationToken ct)
    {
        await UpsertAsync(projection, ct);

        if (projection.RevivesLead)
            await ReviveAsync(projection.FlowId, ct);
    }

    /// <summary>
    /// Undoes an abandonment the visitor has just disproved by acting. Scoped to
    /// abandoned leads by the filter, so a completed purchase is never reopened.
    /// </summary>
    private Task ReviveAsync(string flowId, CancellationToken ct) =>
        _collection.UpdateOneAsync(
            Builders<Lead>.Filter.And(
                Builders<Lead>.Filter.Eq(l => l.Id, flowId),
                Builders<Lead>.Filter.Eq(l => l.Status, LeadStatus.Abandoned)),
            Builders<Lead>.Update
                .Set(l => l.Status, LeadStatus.Active)
                .Set(l => l.AbandonedAt, null),
            cancellationToken: ct);

    private Task UpsertAsync(LeadProjection projection, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var set = new BsonDocument();
        foreach (var (path, value) in projection.Fields)
        {
            // Never write nulls: a later event with a missing field must not
            // erase what an earlier one already recorded.
            if (value is not null)
                set[path] = BsonValue.Create(value);
        }
        set[nameof(Lead.UpdatedAt)] = now;

        var setOnInsert = new BsonDocument { [nameof(Lead.CreatedAt)] = now };
        if (projection.CompanyToken is not null)
            setOnInsert[nameof(Lead.CompanyToken)] = projection.CompanyToken;
        if (projection.SessionId is not null)
            setOnInsert[nameof(Lead.SessionId)] = projection.SessionId;

        var update = new BsonDocument();

        if (projection.Status is not null)
            set[nameof(Lead.Status)] = projection.Status;
        else
            setOnInsert[nameof(Lead.Status)] = LeadStatus.Active;

        if (projection.MinLastStep is int step)
            update["$max"] = new BsonDocument(nameof(Lead.LastStep), step);
        else
            setOnInsert[nameof(Lead.LastStep)] = 0;

        update["$set"] = set;
        update["$setOnInsert"] = setOnInsert;

        var filter = Builders<Lead>.Filter.Eq(l => l.Id, projection.FlowId);
        if (projection.OnlyIfNotCompleted)
            filter &= Builders<Lead>.Filter.Ne(l => l.Status, LeadStatus.Completed);

        return _collection.UpdateOneAsync(
            filter,
            new BsonDocumentUpdateDefinition<Lead>(update),
            // A guarded projection must never create a lead: with the extra
            // condition in the filter an upsert would try to insert a duplicate _id.
            new UpdateOptions { IsUpsert = !projection.OnlyIfNotCompleted },
            ct);
    }

    public async Task<long> MarkStaleAsAbandonedAsync(DateTime inactiveBefore, CancellationToken ct)
    {
        var filter = Builders<Lead>.Filter.And(
            Builders<Lead>.Filter.Eq(l => l.Status, LeadStatus.Active),
            Builders<Lead>.Filter.Lt(l => l.UpdatedAt, inactiveBefore));

        var now = DateTime.UtcNow;
        var update = Builders<Lead>.Update
            .Set(l => l.Status, LeadStatus.Abandoned)
            .Set(l => l.AbandonedAt, now)
            .Set(l => l.UpdatedAt, now);

        var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount;
    }
}
