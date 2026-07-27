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
    /// Marks the transaction Confirmed and returns whether this caller was the one
    /// that did it — the guard against publishing two emission jobs for one
    /// payment, now that a webhook and two reconciliation paths can all confirm it.
    ///
    /// Matches anything that is not ALREADY Confirmed rather than only Pending, on
    /// purpose. A buyer whose first card is rejected retries with another one, and a
    /// cash coupon can be paid after the abandonment sweep has given up on it: both
    /// arrive as an approval for a transaction that is Failed or Abandoned, and
    /// refusing those would take money without issuing a policy.
    /// </summary>
    Task<bool> TryMarkConfirmedAsync(string id, DateTime confirmedAt, CancellationToken ct);

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
