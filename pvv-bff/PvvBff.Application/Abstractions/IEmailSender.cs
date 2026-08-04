namespace PvvBff.Application.Abstractions;

/// <summary>
/// One outgoing email. Carries both bodies on purpose: the HTML is what the reader
/// is meant to see, and the plain text is what mail clients that refuse HTML — and
/// spam filters, which distrust a message that has only one — fall back to.
/// </summary>
/// <param name="To">Recipient address.</param>
/// <param name="ToName">Recipient display name, when we know it.</param>
public sealed record EmailMessage(
    string To,
    string? ToName,
    string Subject,
    string HtmlBody,
    string TextBody);

/// <summary>Outcome of an attempt to hand a message to the mail provider.</summary>
/// <param name="Sent">
/// Whether the provider accepted it. Accepted is not delivered — nothing observable
/// from here can promise an inbox — but it is the last point at which this system
/// still has a say.
/// </param>
public sealed record EmailResult(bool Sent, string? Error = null);

/// <summary>
/// Sends transactional email. Behind this sit a real SMTP client and a mock that only
/// logs, chosen by <c>Email:Provider</c> — the same shape as <see cref="IPaymentGateway"/>,
/// and for the same reason: the system has to stay demonstrable with no credentials
/// and no network.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Hands the message to the provider. Implementations do not throw on a refused
    /// send — a lead that could not be emailed is an outcome to record, not an
    /// exception to unwind an operator's request with.
    /// </summary>
    Task<EmailResult> SendAsync(EmailMessage message, CancellationToken ct);
}
