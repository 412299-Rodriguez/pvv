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
    /// Claims the right to project this transaction's emission outcome onto its
    /// lead, and returns whether the caller won it.
    ///
    /// The claim is the write itself — it only matches a transaction that has
    /// not been stamped yet — so concurrent status polls cannot both decide they
    /// are the first. Checking a flag and then setting it would let them.
    /// </summary>
    Task<bool> TryClaimEmissionProjectionAsync(string id, DateTime at, CancellationToken ct);
}
