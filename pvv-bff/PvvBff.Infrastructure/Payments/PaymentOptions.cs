namespace PvvBff.Infrastructure.Payments;

/// <summary>Payment settings, bound from "Payments".</summary>
public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    /// <summary>
    /// Page the mock init_point points to (our own checkout stand-in). HU-11
    /// replaces this with Mercado Pago's real init_point.
    /// </summary>
    public string MockCheckoutBaseUrl { get; set; } = "http://localhost:5173/mock-checkout";
}
