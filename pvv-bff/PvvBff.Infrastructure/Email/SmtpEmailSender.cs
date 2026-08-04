using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using PvvBff.Application.Abstractions;

namespace PvvBff.Infrastructure.Email;

/// <summary>
/// Sends through an SMTP server (MailKit).
///
/// A connection per message rather than a pooled client: this system sends one email
/// when an operator presses a button, so the cost of connecting is irrelevant next to
/// the cost of holding an authenticated session open against a provider that will drop
/// it anyway.
/// </summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailResult> SendAsync(EmailMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            // Misconfiguration, not a failed send: say so instead of reporting a
            // provider error that never happened.
            _logger.LogError("SMTP is selected but Email:Host or Email:FromAddress is empty");
            return new EmailResult(false, "El envío de correo no está configurado.");
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(new MailboxAddress(message.ToName ?? string.Empty, message.To));
        mime.Subject = message.Subject;

        if (!string.IsNullOrWhiteSpace(_options.ReplyTo))
            mime.ReplyTo.Add(MailboxAddress.Parse(_options.ReplyTo));

        // multipart/alternative: the reader gets the HTML, and a client that will not
        // render it still gets a readable message instead of nothing.
        mime.Body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
        }.ToMessageBody();

        using var client = new SmtpClient
        {
            Timeout = _options.TimeoutSeconds * 1000,
        };

        try
        {
            var security = _options.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_options.Host, _options.Port, security, ct);

            // A local catcher accepts anything and offers no AUTH at all; asking it to
            // authenticate is an error, not a safety measure.
            if (!string.IsNullOrWhiteSpace(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password, ct);

            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(quit: true, ct);

            _logger.LogInformation("Recovery email accepted by {Host} for {To}", _options.Host, message.To);
            return new EmailResult(true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The address may be wrong, the provider may be down, the credentials may
            // have expired. None of that should tear down the operator's request: the
            // caller records the failure and shows it.
            _logger.LogError(ex, "SMTP send failed for {To}", message.To);
            return new EmailResult(false, "No se pudo enviar el correo.");
        }
    }
}
