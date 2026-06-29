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
    /// as Abandoned. Returns how many were updated.
    /// </summary>
    Task<long> MarkAbandonedAsync(DateTime olderThan, CancellationToken ct);
}
