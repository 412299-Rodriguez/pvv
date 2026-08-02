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

    // Compare-and-set: the filter excludes transactions that are already Confirmed,
    // so of several concurrent approvals exactly one modifies the document and only
    // that one publishes an emission job. Deliberately "not Confirmed" rather than
    // "is Pending" — see the interface for why a Failed or Abandoned transaction
    // must still be allowed to become Confirmed.
    public async Task<bool> TryMarkConfirmedAsync(
        string id, DateTime confirmedAt, string? providerPaymentId, CancellationToken ct)
    {
        var update = Builders<PaymentTransaction>.Update
            .Set(t => t.Status, PaymentStatus.Confirmed)
            .Set(t => t.ConfirmedAt, confirmedAt);

        // Only when the caller actually has one: overwriting a recorded id with null
        // would erase the trail on the second confirmation path to reach the same
        // transaction.
        if (!string.IsNullOrWhiteSpace(providerPaymentId))
            update = update.Set(t => t.ProviderPaymentId, providerPaymentId);

        var result = await _collection.UpdateOneAsync(
            Builders<PaymentTransaction>.Filter.And(
                Builders<PaymentTransaction>.Filter.Eq(t => t.Id, id),
                Builders<PaymentTransaction>.Filter.Ne(t => t.Status, PaymentStatus.Confirmed)),
            update,
            cancellationToken: ct);

        return result.ModifiedCount == 1;
    }

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

    // Targeted update for the same reason as above: the emission worker owns other
    // fields on this document. Only annotates a transaction that is still Pending.
    public Task MarkPendingPaymentAsync(
        string id, string providerPaymentId, DateTime? pendingUntil, CancellationToken ct) =>
        _collection.UpdateOneAsync(
            Builders<PaymentTransaction>.Filter.And(
                Builders<PaymentTransaction>.Filter.Eq(t => t.Id, id),
                Builders<PaymentTransaction>.Filter.Eq(t => t.Status, PaymentStatus.Pending)),
            Builders<PaymentTransaction>.Update
                .Set(t => t.ProviderPaymentId, providerPaymentId)
                .Set(t => t.PaymentPendingUntil, pendingUntil),
            cancellationToken: ct);

    public async Task<IReadOnlyList<PaymentTransaction>> GetReconcilableAsync(
        DateTime createdAfter, CancellationToken ct)
    {
        var filter = Builders<PaymentTransaction>.Filter.And(
            Builders<PaymentTransaction>.Filter.Eq(t => t.Status, PaymentStatus.Pending),
            Builders<PaymentTransaction>.Filter.Or(
                Builders<PaymentTransaction>.Filter.Gte(t => t.CreatedAt, createdAfter),
                // Age is the wrong filter for a coupon: it is supposed to sit unpaid
                // for weeks, so having a payment at all keeps it in scope.
                Builders<PaymentTransaction>.Filter.Ne(t => t.ProviderPaymentId, null)));

        return await (await _collection.FindAsync(filter, cancellationToken: ct)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PaymentTransaction>> MarkAbandonedAsync(
        DateTime createdBefore, DateTime asOf, CancellationToken ct)
    {
        var filter = Builders<PaymentTransaction>.Filter.And(
            Builders<PaymentTransaction>.Filter.Eq(t => t.Status, PaymentStatus.Pending),
            Builders<PaymentTransaction>.Filter.Lt(t => t.CreatedAt, createdBefore),
            // Holding a payable coupon is the opposite of having abandoned the
            // purchase. Only sweep once that deadline has actually passed.
            Builders<PaymentTransaction>.Filter.Or(
                Builders<PaymentTransaction>.Filter.Eq(t => t.PaymentPendingUntil, null),
                Builders<PaymentTransaction>.Filter.Lt(t => t.PaymentPendingUntil, asOf)));

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
