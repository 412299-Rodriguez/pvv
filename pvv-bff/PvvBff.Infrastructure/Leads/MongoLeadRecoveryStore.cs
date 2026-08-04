using MongoDB.Driver;
using PvvBff.Application.Abstractions;
using PvvBff.Domain.Leads;

namespace PvvBff.Infrastructure.Leads;

/// <summary>Recovery reads and writes against the <c>leads</c> collection.</summary>
public sealed class MongoLeadRecoveryStore : ILeadRecoveryStore
{
    private readonly IMongoCollection<Lead> _collection;

    public MongoLeadRecoveryStore(IMongoDatabase database) =>
        _collection = database.GetCollection<Lead>("leads");

    public async Task<Lead?> GetScopedAsync(string flowId, string companyToken, CancellationToken ct)
    {
        // The company is part of the filter, not a check on the result: a lead from
        // another tenant is not "found and rejected", it simply does not exist here.
        var filter = Builders<Lead>.Filter.And(
            Builders<Lead>.Filter.Eq(l => l.Id, flowId),
            Builders<Lead>.Filter.Eq(l => l.CompanyToken, companyToken));

        return await (await _collection.FindAsync(filter, cancellationToken: ct))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> TryClaimRecoveryAsync(
        string flowId, DateTime at, string by, CancellationToken ct)
    {
        // Compare-and-set: matching only an unrecovered lead means the claim and the
        // write are the same operation, so two concurrent callers cannot both win.
        var result = await _collection.UpdateOneAsync(
            Builders<Lead>.Filter.And(
                Builders<Lead>.Filter.Eq(l => l.Id, flowId),
                Builders<Lead>.Filter.Eq(l => l.RecoveredAt, null)),
            Builders<Lead>.Update
                .Set(l => l.RecoveredAt, at)
                .Set(l => l.RecoveredBy, by),
            cancellationToken: ct);

        return result.ModifiedCount == 1;
    }

    public Task ReleaseRecoveryClaimAsync(string flowId, CancellationToken ct) =>
        _collection.UpdateOneAsync(
            Builders<Lead>.Filter.Eq(l => l.Id, flowId),
            Builders<Lead>.Update
                .Set(l => l.RecoveredAt, (DateTime?)null)
                .Set(l => l.RecoveredBy, (string?)null),
            cancellationToken: ct);
}
