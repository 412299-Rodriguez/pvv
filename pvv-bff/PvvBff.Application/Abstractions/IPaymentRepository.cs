using PvvBff.Domain.Payments;

namespace PvvBff.Application.Abstractions;

/// <summary>Persistence for payment transactions (MongoDB).</summary>
public interface IPaymentRepository
{
    Task AddAsync(PaymentTransaction transaction, CancellationToken ct);

    Task<PaymentTransaction?> GetAsync(string id, CancellationToken ct);

    Task UpdateAsync(PaymentTransaction transaction, CancellationToken ct);

    /// <summary>
    /// Records that the provider has a payment for this transaction which has not been
    /// completed yet — a cash coupon or a transfer — together with its deadline.
    /// </summary>
    Task MarkPendingPaymentAsync(
        string id, string providerPaymentId, DateTime? pendingUntil, CancellationToken ct);

    /// <summary>
    /// Pending transactions still worth asking the provider about: those created after
    /// <paramref name="createdAfter"/>, plus any with a payment awaiting completion
    /// regardless of age — a cash coupon must not fall out of the window just because
    /// it is three weeks old.
    /// </summary>
    Task<IReadOnlyList<PaymentTransaction>> GetReconcilableAsync(
        DateTime createdAfter, CancellationToken ct);

    /// <summary>
    /// Marks still-Pending transactions created before <paramref name="createdBefore"/>
    /// as Abandoned, and returns them so their leads can be projected too.
    ///
    /// A transaction whose pending payment is still payable as of <paramref name="asOf"/>
    /// is deliberately left alone: the buyer is holding a coupon, which is the opposite
    /// of having walked away.
    /// </summary>
    Task<IReadOnlyList<PaymentTransaction>> MarkAbandonedAsync(
        DateTime createdBefore, DateTime asOf, CancellationToken ct);

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
    /// <param name="providerPaymentId">
    /// The provider's id for the payment that settled this transaction, when the caller
    /// knows it. Stamped together with the confirmation rather than separately, because
    /// it is the only link back from a policy we issued to the payment that paid for it —
    /// what a refund, a chargeback or a support question all start from. Null leaves any
    /// previously recorded id untouched (the mock gateway reports no id at all).
    /// </param>
    Task<bool> TryMarkConfirmedAsync(
        string id, DateTime confirmedAt, string? providerPaymentId, CancellationToken ct);

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
