namespace PvvBff.Infrastructure.Payments;

/// <summary>
/// Mercado Pago settings, bound from "MercadoPago".
///
/// The two credentials default to empty on purpose: they live in
/// <c>dotnet user-secrets</c>, outside the repository, and the tracked
/// appsettings files only carry placeholders. Nothing here should ever hold a
/// real value.
/// </summary>
public sealed class MercadoPagoOptions
{
    public const string SectionName = "MercadoPago";

    /// <summary>Access token of the account that creates the preferences.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Webhook signing key. Used from phase C to verify notifications.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://api.mercadopago.com";

    /// <summary>
    /// Origin the buyer is returned to after the checkout. The transaction id is
    /// appended, which is what lets the result page find its transaction again
    /// with no session state.
    /// </summary>
    public string BackUrlBase { get; set; } = "http://localhost:5173";

    /// <summary>
    /// Public URL Mercado Pago POSTs its notifications to. Empty until the dev
    /// tunnel is running, in which case the field is omitted from the preference
    /// rather than sent as something unreachable.
    /// </summary>
    public string NotificationUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether to ask Mercado Pago to bounce the buyer back automatically once
    /// the payment is approved.
    ///
    /// Configurable because Mercado Pago validates <c>back_urls</c> when this is
    /// on and may refuse a localhost origin. Turning it off costs the buyer one
    /// click on "Volver al sitio" and nothing else, so it is the cheap way out
    /// if that validation bites.
    /// </summary>
    public bool AutoReturn { get; set; } = true;

    /// <summary>
    /// Send the buyer to the sandbox checkout instead of the production one.
    ///
    /// Must be true whenever the access token belongs to a test account. Mercado
    /// Pago returns two different hosts for a preference — sandbox.mercadopago.com.ar
    /// and www.mercadopago.com.ar — and it refuses a payment whose collector and
    /// checkout are in different environments.
    /// </summary>
    public bool UseSandbox { get; set; }

    public string CurrencyId { get; set; } = "ARS";

    /// <summary>How the charge is labelled on the buyer's card statement.</summary>
    public string StatementDescriptor { get; set; } = "PVV SEGUROS";

    /// <summary>
    /// How long the checkout preference stays payable.
    ///
    /// Deliberately NOT reusing <c>Payments:AbandonmentTtlMinutes</c>: that one is
    /// tuned down hard in development (3 minutes) to exercise the abandonment
    /// sweep, which is far less than a person needs to type a card number.
    /// </summary>
    public int PreferenceExpiryMinutes { get; set; } = 30;
}
