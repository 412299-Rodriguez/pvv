using Microsoft.Extensions.Logging;
using PvvBff.Application.Abstractions;

namespace PvvBff.Infrastructure.Email;

/// <summary>
/// Writes the message to the log and reports success. Selected by
/// <c>Email:Provider = Mock</c>.
///
/// Not dead code: it is what keeps lead recovery demonstrable with no SMTP server, no
/// credentials and no internet — the same reason the mock payment gateway survived
/// HU-11. It logs the subject and recipient but NOT the body, which carries a real
/// person's name, plate and quote.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task<EmailResult> SendAsync(EmailMessage message, CancellationToken ct)
    {
        _logger.LogInformation(
            "[mock email] to {To} — subject: {Subject} ({HtmlLength} chars of HTML)",
            message.To, message.Subject, message.HtmlBody.Length);

        return Task.FromResult(new EmailResult(true));
    }
}
