namespace PvvConfig.Application.DTOs.ConfigurationValues;

/// <summary>
/// Copy of the recovery email a company sends to visitors who abandoned a purchase
/// (configuration type <c>RECOVERY_EMAIL_CONFIG</c>, read by pvv-bff).
///
/// Text only, on purpose. The layout, the branding and the button belong to the BFF's
/// renderer: handing an operator raw HTML means an unclosed tag reaches a customer's
/// inbox, and it makes the person editing a sentence responsible for how email clients
/// render tables.
///
/// Any field may contain the placeholders <c>{nombre}</c>, <c>{patente}</c>,
/// <c>{vehiculo}</c>, <c>{producto}</c>, <c>{precio}</c> and <c>{compania}</c>.
/// </summary>
public class RecoveryEmailConfigValueDto
{
    public string Subject { get; set; } = string.Empty;

    public string Greeting { get; set; } = string.Empty;

    public string Intro { get; set; } = string.Empty;

    public string ButtonLabel { get; set; } = string.Empty;

    public string Closing { get; set; } = string.Empty;
}
