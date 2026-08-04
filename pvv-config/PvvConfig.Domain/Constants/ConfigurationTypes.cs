namespace PvvConfig.Domain.Constants;

/// <summary>Well-known configuration type discriminators (EAV model).</summary>
public static class ConfigurationTypes
{
    public const string PVV_UI_CONFIG = "PVV_UI_CONFIG";
    public const string PRODUCT_CONFIG = "PRODUCT_CONFIG";
    public const string PRICING_CONFIG = "PRICING_CONFIG";

    /// <summary>Copy of the abandoned-cart recovery email (HU-12).</summary>
    public const string RECOVERY_EMAIL_CONFIG = "RECOVERY_EMAIL_CONFIG";
    public const string MAINTENANCE_MODE = "MAINTENANCE_MODE";
    public const string EMISSION_QUEUE_CONFIG = "EMISSION_QUEUE_CONFIG";
}
