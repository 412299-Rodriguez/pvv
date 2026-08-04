namespace PvvBff.Application.Leads.Recovery;

/// <summary>
/// The editable part of a recovery email (config type <c>RECOVERY_EMAIL_CONFIG</c>).
///
/// Deliberately four pieces of TEXT rather than a blob of HTML. Handing an operator raw
/// markup means one unclosed tag turns a branded email into garbage in somebody's inbox,
/// and it puts the burden of responsive email layout on the person editing copy. The
/// layout, the branding and the button belong to the renderer; the words belong to the
/// company.
///
/// Any field may use the placeholders listed in <see cref="RecoveryPlaceholders"/>.
/// </summary>
public sealed class RecoveryEmailTemplate
{
    public string Subject { get; set; } = "Tu cotización de {compania} sigue disponible";

    /// <summary>Opening line. Greets by name when we have one.</summary>
    public string Greeting { get; set; } = "Hola {nombre},";

    /// <summary>The reason for writing. Shown as the main paragraph.</summary>
    public string Intro { get; set; } =
        "Vimos que empezaste a cotizar el seguro de tu {vehiculo} y no llegaste a " +
        "completar la compra. Tu cotización sigue disponible y podés retomarla cuando quieras.";

    /// <summary>Label of the button that leads back to the portal.</summary>
    public string ButtonLabel { get; set; } = "Retomar mi compra";

    /// <summary>Closing line, under the button.</summary>
    public string Closing { get; set; } =
        "Si ya contrataste tu seguro por otro medio, ignorá este mensaje.";

    /// <summary>
    /// Fills in whatever the company left blank.
    ///
    /// Per field rather than all-or-nothing: a company that customised only the subject
    /// has still customised its email, and an operator who cleared a box to retype it
    /// and then saved should not be able to send a message with a hole in the middle.
    /// </summary>
    public static RecoveryEmailTemplate Merge(RecoveryEmailTemplate? configured)
    {
        var defaults = new RecoveryEmailTemplate();
        if (configured is null)
            return defaults;

        return new RecoveryEmailTemplate
        {
            Subject = Pick(configured.Subject, defaults.Subject),
            Greeting = Pick(configured.Greeting, defaults.Greeting),
            Intro = Pick(configured.Intro, defaults.Intro),
            ButtonLabel = Pick(configured.ButtonLabel, defaults.ButtonLabel),
            Closing = Pick(configured.Closing, defaults.Closing),
        };

        static string Pick(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}

/// <summary>
/// What an operator may drop into the template text. Everything here comes from what
/// the lead itself told us — there is no placeholder for data we do not have.
/// </summary>
public static class RecoveryPlaceholders
{
    public const string Name = "{nombre}";
    public const string Plate = "{patente}";
    public const string Vehicle = "{vehiculo}";
    public const string Product = "{producto}";
    public const string Price = "{precio}";
    public const string Company = "{compania}";
}
