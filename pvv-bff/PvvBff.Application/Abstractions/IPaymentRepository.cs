using PvvBff.Domain.Payments;

namespace PvvBff.Application.Abstractions;

/// <summary>Persistence for payment transactions (MongoDB).</summary>
public interface IPaymentRepository
{
    Task AddAsync(PaymentTransaction transaction, CancellationToken ct);

    Task<PaymentTransaction?> GetAsync(string id, CancellationToken ct);

    Task UpdateAsync(PaymentTransaction transaction, CancellationToken ct);

    /// <summary>
    /// Marks every still-Pending transaction created before <paramref name="olderThan"/>
    /// as Abandoned. Returns the transactions that were marked, so their leads can
    /// be projected too.
    /// </summary>
    Task<IReadOnlyList<PaymentTransaction>> MarkAbandonedAsync(DateTime olderThan, CancellationToken ct);

    /// <summary>
    /// Stamps the transaction as having had its emission outcome projected onto
    /// the lead, so repeated status polls do not project it again.
    /// </summary>
    Task MarkEmissionProjectedAsync(string id, DateTime at, CancellationToken ct);
}
