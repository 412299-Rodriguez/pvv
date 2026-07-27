using MongoDB.Driver;
using PvvBff.Application.Abstractions;
using PvvBff.Domain.Payments;

namespace PvvBff.Infrastructure.Payments;

/// <summary>Stores payment transactions in MongoDB (collection payment_transactions).</summary>
public sealed class MongoPaymentRepository : IPaymentRepository
{
    private readonly IMongoCollection<PaymentTransaction> _collection;

    public MongoPaymentRepository(IMongoDatabase database)
    {
        _collection = database.GetCollection<PaymentTransaction>("payment_transactions");
    }

    public Task AddAsync(PaymentTransaction transaction, CancellationToken ct) =>
        _collection.InsertOneAsync(transaction, cancellationToken: ct);

    public async Task<PaymentTransaction?> GetAsync(string id, CancellationToken ct) =>
        await (await _collection.FindAsync(t => t.Id == id, cancellationToken: ct)).FirstOrDefaultAsync(ct);

    public Task UpdateAsync(PaymentTransaction transaction, CancellationToken ct) =>
        _collection.ReplaceOneAsync(t => t.Id == transaction.Id, transaction, cancellationToken: ct);

    // Targeted update rather than a full replace: the emission worker writes its
    // own fields on this same document. Matching only an unstamped transaction
    // makes this a compare-and-set — exactly one concurrent caller modifies it.
    public async Task<bool> TryClaimEmissionProjectionAsync(string id, DateTime at, CancellationToken ct)
    {
        var result = await _collection.UpdateOneAsync(
            Builders<PaymentTransaction>.Filter.And(
                Builders<PaymentTransaction>.Filter.Eq(t => t.Id, id),
                Builders<PaymentTransaction>.Filter.Eq(t => t.EmissionProjectedAt, null)),
            Builders<PaymentTransaction>.Update.Set(t => t.EmissionProjectedAt, at),
            cancellationToken: ct);

        return result.ModifiedCount == 1;
    }

    public async Task<IReadOnlyList<PaymentTransaction>> MarkAbandonedAsync(DateTime olderThan, CancellationToken ct)
    {
        var filter = Builders<PaymentTransaction>.Filter.And(
            Builders<PaymentTransaction>.Filter.Eq(t => t.Status, PaymentStatus.Pending),
            Builders<PaymentTransaction>.Filter.Lt(t => t.CreatedAt, olderThan));

        // Read them first: the caller needs the flow ids to project their leads.
        var stale = await (await _collection.FindAsync(filter, cancellationToken: ct)).ToListAsync(ct);
        if (stale.Count == 0)
            return [];

        var ids = stale.Select(t => t.Id).ToList();
        await _collection.UpdateManyAsync(
            Builders<PaymentTransaction>.Filter.In(t => t.Id, ids),
            Builders<PaymentTransaction>.Update.Set(t => t.Status, PaymentStatus.Abandoned),
            cancellationToken: ct);

        return stale;
    }
}
