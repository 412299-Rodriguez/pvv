namespace PvvBff.Infrastructure.Email;

/// <summary>Which sender implementation is wired up.</summary>
public enum EmailProviderKind
{
    /// <summary>Writes the message to the log and reports success. No SMTP, no network.</summary>
    Mock = 0,

    /// <summary>Real SMTP. Any provider speaks it — Brevo, a local catcher, a corporate relay.</summary>
    Smtp = 1,
}

/// <summary>
/// Mail settings (configuration section <c>Email</c>).
///
/// Deliberately SMTP rather than one provider's HTTP API: SMTP is the same protocol
/// everywhere, so moving between a local catcher and a real provider is a change of
/// host and credentials, not of code.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public EmailProviderKind Provider { get; set; } = EmailProviderKind.Mock;

    /// <summary>
    /// Address the mail is sent from. With most providers this has to be an address
    /// you have verified with them; an unverified sender is refused or lands in spam.
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the sender. Deliberately the PLATFORM's name and not the
    /// tenant's: the message leaves our infrastructure and our verified address, and
    /// signing it with an insurer's name would be claiming to be them. The tenant's
    /// brand belongs inside the message, where the reader can see who the quote was
    /// with, and it comes from that company's own UI configuration.
    /// </summary>
    public string FromName { get; set; } = "PVV Seguros";

    /// <summary>Where a reply goes. Left empty, replies go to <see cref="FromAddress"/>.</summary>
    public string? ReplyTo { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// STARTTLS on the submission port (587), which is what providers expect. Turn it
    /// off only for a local catcher, which speaks plain SMTP and has nothing to protect.
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 20;

    /// <summary>
    /// Public base URL of the buyer portal, used to build the link in the recovery
    /// email. It has to be reachable from outside this machine: a recipient clicking
    /// <c>localhost</c> lands on their own computer.
    /// </summary>
    public string PortalBaseUrl { get; set; } = "http://localhost:5173";
}
