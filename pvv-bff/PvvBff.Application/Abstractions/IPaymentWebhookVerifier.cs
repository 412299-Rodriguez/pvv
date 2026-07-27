namespace PvvBff.Application.Abstractions;

/// <summary>
/// Checks that a provider notification really came from the provider.
///
/// Worth being clear about how much this is load-bearing. The notification is only a
/// doorbell: it carries a payment id and nothing we trust, and the payment itself is
/// then read back from the provider's API over an authenticated call. So a forged
/// notification cannot invent a payment. What the signature buys is that a stranger who
/// discovers the URL cannot make us hammer the provider's API with invented ids, and it
/// is what the provider expects an integration to do.
/// </summary>
public interface IPaymentWebhookVerifier
{
    /// <summary>
    /// False when no signing secret is configured. The endpoint must fail closed in that
    /// case rather than accept unverified notifications.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Whether the signature covers this notification and is recent enough to not be a
    /// replay.
    /// </summary>
    bool Verify(string providerPaymentId, string? signatureHeader, string? requestId);
}
