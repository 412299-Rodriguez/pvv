namespace PvvBff.Infrastructure.Payments;

/// <summary>Which payment provider implementation gets registered.</summary>
public enum PaymentGatewayKind
{
    /// <summary>
    /// Our own /mock-checkout page standing in for the provider. Kept after
    /// HU-11 so the purchase flow can still be demonstrated with no Mercado Pago
    /// credentials and no internet.
    /// </summary>
    Mock,

    /// <summary>Real Mercado Pago Checkout Pro (HU-11).</summary>
    MercadoPago,
}

/// <summary>Payment settings, bound from "Payments".</summary>
public sealed class PaymentOptions
{
    public const string SectionName = "Payments";

    /// <summary>Which gateway to use. See <see cref="PaymentGatewayKind"/>.</summary>
    public PaymentGatewayKind Gateway { get; set; } = PaymentGatewayKind.Mock;

    /// <summary>
    /// Page the mock init_point points to (our own checkout stand-in). Unused
    /// when the gateway is MercadoPago, which returns its own init_point.
    /// </summary>
    public string MockCheckoutBaseUrl { get; set; } = "http://localhost:5173/mock-checkout";

    /// <summary>A Pending transaction older than this is considered abandoned.</summary>
    public int AbandonmentTtlMinutes { get; set; } = 30;

    /// <summary>How often the abandonment job scans for stale transactions.</summary>
    public int AbandonmentScanSeconds { get; set; } = 300;
}
