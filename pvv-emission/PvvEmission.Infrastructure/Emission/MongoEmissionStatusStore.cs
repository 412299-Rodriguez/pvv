using MongoDB.Bson;
using MongoDB.Driver;
using PvvEmission.Application.Emission;

namespace PvvEmission.Infrastructure.Emission;

/// <summary>
/// Writes the emission outcome onto the BFF's payment transaction (collection
/// payment_transactions in the shared pvv_bff_db), keyed by transaction id.
/// Uses BsonDocument so this service needn't share the BFF's entity.
/// </summary>
public sealed class MongoEmissionStatusStore : IEmissionStatusStore
{
    private readonly IMongoCollection<BsonDocument> _transactions;

    public MongoEmissionStatusStore(IMongoDatabase database)
    {
        _transactions = database.GetCollection<BsonDocument>("payment_transactions");
    }

    public Task MarkEmittingAsync(string transactionId, int attempt, CancellationToken ct) =>
        UpdateAsync(transactionId, "emitting", policyNumber: null, attempt, ct);

    public Task MarkSuccessAsync(string transactionId, string? policyNumber, int attempt, CancellationToken ct) =>
        UpdateAsync(transactionId, "success", policyNumber, attempt, ct);

    public Task MarkFailedAsync(string transactionId, string status, int attempt, CancellationToken ct) =>
        UpdateAsync(transactionId, status, policyNumber: null, attempt, ct);

    private Task UpdateAsync(string transactionId, string status, string? policyNumber, int attempt, CancellationToken ct)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", transactionId);
        var update = Builders<BsonDocument>.Update
            .Set("EmissionStatus", status)
            .Set("EmissionAttempts", attempt)
            .Set("EmissionUpdatedAt", DateTime.UtcNow);

        if (policyNumber is not null)
            update = update.Set("PolicyNumber", policyNumber);

        return _transactions.UpdateOneAsync(filter, update, cancellationToken: ct);
    }
}
