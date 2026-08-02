using PvvBff.Domain.Payments;

namespace PvvBff.Application.Payments;

/// <summary>
/// Scoping rule for the transaction lookups the portal exposes (PAYMENT_SYNC and
/// EMISSION_STATUS): a transaction is readable only through the portal it was
/// created in.
///
/// Worth being precise about what this is NOT. The company token travels in a
/// client header and is public — it is printed in the portal's own URL — so a
/// caller can set it to whatever they like. This is a scoping guard and a defence
/// in depth, not an authorization boundary. What actually keeps a stranger from
/// reading someone's transaction is that its id is a 128-bit value nobody can guess.
/// </summary>
public static class PaymentAccess
{
    public static bool BelongsTo(PaymentTransaction transaction, string? companyToken) =>
        // A transaction with no token predates the tenant it would be scoped to;
        // there is nothing to confuse it with, so it stays readable.
        string.IsNullOrWhiteSpace(transaction.CompanyToken)
        || string.Equals(transaction.CompanyToken, companyToken, StringComparison.Ordinal);
}
