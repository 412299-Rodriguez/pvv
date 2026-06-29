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

    public async Task<long> MarkAbandonedAsync(DateTime olderThan, CancellationToken ct)
    {
        var filter = Builders<PaymentTransaction>.Filter.And(
            Builders<PaymentTransaction>.Filter.Eq(t => t.Status, PaymentStatus.Pending),
            Builders<PaymentTransaction>.Filter.Lt(t => t.CreatedAt, olderThan));
        var update = Builders<PaymentTransaction>.Update.Set(t => t.Status, PaymentStatus.Abandoned);

        var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount;
    }
}
