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
}
