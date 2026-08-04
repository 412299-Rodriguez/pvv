using System.Globalization;
using System.Net;
using System.Text;
using PvvBff.Application.Abstractions;
using PvvBff.Domain.Leads;

namespace PvvBff.Application.Leads.Recovery;

/// <summary>The tenant's look, as the email needs it.</summary>
public sealed record RecoveryBranding(string CompanyName, string? LogoUrl, string PrimaryColor);

/// <summary>
/// Turns a lead, a template and a company's branding into the message that gets sent.
///
/// Rendering happens HERE and not in the browser. Until now the admin panel built the
/// body itself and handed it to a `mailto:` link, which meant the text that reached a
/// customer was whatever the operator's mail client had at that moment. Once the system
/// is the one sending, it has to be the one deciding what it says.
/// </summary>
public static class RecoveryEmailRenderer
{
    /// <summary>Fallback when the company's palette is missing or unusable.</summary>
    private const string FallbackColor = "#0071ce";

    public static EmailMessage Render(
        Lead lead,
        RecoveryEmailTemplate template,
        RecoveryBranding branding,
        string portalUrl)
    {
        var values = BuildValues(lead, branding);

        var subject = Fill(template.Subject, values);
        var greeting = Fill(template.Greeting, values);
        var intro = Fill(template.Intro, values);
        var closing = Fill(template.Closing, values);
        var button = Fill(template.ButtonLabel, values);

        var color = Sanitize(branding.PrimaryColor);
        var html = BuildHtml(greeting, intro, button, closing, branding, color, portalUrl, lead);
        var text = BuildText(greeting, intro, closing, portalUrl, branding);

        return new EmailMessage(
            To: lead.ContactEmail ?? string.Empty,
            ToName: DisplayName(lead),
            Subject: subject,
            HtmlBody: html,
            TextBody: text);
    }

    private static Dictionary<string, string> BuildValues(Lead lead, RecoveryBranding branding)
    {
        var plate = lead.Steps.Step1.Plate ?? string.Empty;
        var vehicle = lead.Steps.Step1.VehicleTitle
            ?? (string.IsNullOrWhiteSpace(plate) ? "vehículo" : $"vehículo {plate}");

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // Only the first name: a recovery email that opens with somebody's full
            // legal name reads like a debt collection notice.
            [RecoveryPlaceholders.Name] = FirstName(lead),
            [RecoveryPlaceholders.Plate] = plate,
            [RecoveryPlaceholders.Vehicle] = vehicle,
            [RecoveryPlaceholders.Product] = lead.Steps.Step3.ProductName ?? string.Empty,
            [RecoveryPlaceholders.Price] = FormatPrice(lead.Steps.Step3.Amount),
            [RecoveryPlaceholders.Company] = branding.CompanyName,
        };
    }

    private static string Fill(string template, Dictionary<string, string> values)
    {
        var result = new StringBuilder(template);
        foreach (var (key, value) in values)
            result.Replace(key, value);

        // A greeting whose name was empty leaves "Hola ," behind. Tidy the seams rather
        // than forbidding the placeholder.
        return result.ToString().Replace(" ,", ",").Replace("  ", " ").Trim();
    }

    private static string FirstName(Lead lead)
    {
        var first = lead.Steps.Step2.FirstName;
        if (!string.IsNullOrWhiteSpace(first))
            return first.Trim().Split(' ')[0];

        return string.Empty;
    }

    private static string? DisplayName(Lead lead)
    {
        var full = $"{lead.Steps.Step2.FirstName} {lead.Steps.Step2.LastName}".Trim();
        return string.IsNullOrWhiteSpace(full) ? null : full;
    }

    private static string FormatPrice(decimal? amount) =>
        amount is null
            ? string.Empty
            : amount.Value.ToString("C0", CultureInfo.GetCultureInfo("es-AR"));

    /// <summary>
    /// The colour is interpolated straight into a style attribute, so it is allowed to
    /// be a hex colour and nothing else. It arrives from tenant configuration, which an
    /// operator edits — that makes it untrusted input on its way into markup.
    /// </summary>
    private static string Sanitize(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return FallbackColor;

        var value = color.Trim();
        if (value.Length is not (4 or 7) || value[0] != '#')
            return FallbackColor;

        for (var i = 1; i < value.Length; i++)
        {
            if (!Uri.IsHexDigit(value[i]))
                return FallbackColor;
        }

        return value;
    }

    private static string BuildHtml(
        string greeting,
        string intro,
        string button,
        string closing,
        RecoveryBranding branding,
        string color,
        string portalUrl,
        Lead lead)
    {
        // Tables and inline styles, not flexbox and a stylesheet: email clients are a
        // decade behind browsers and Outlook still renders with Word's engine.
        var logo = string.IsNullOrWhiteSpace(branding.LogoUrl)
            ? $"<div style=\"font:600 20px system-ui,sans-serif;color:{color}\">{Esc(branding.CompanyName)}</div>"
            : $"<img src=\"{Esc(branding.LogoUrl)}\" alt=\"{Esc(branding.CompanyName)}\" height=\"40\" style=\"height:40px;display:block;border:0\">";

        var quote = BuildQuoteBlock(lead, color);

        return $"""
        <!doctype html>
        <html lang="es"><head><meta charset="utf-8">
        <meta name="viewport" content="width=device-width,initial-scale=1">
        <title>{Esc(branding.CompanyName)}</title></head>
        <body style="margin:0;padding:0;background:#f4f4f5">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:24px 12px">
        <tr><td align="center">
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:#ffffff;border-radius:12px;overflow:hidden">
          <tr><td style="padding:28px 32px 8px 32px">{logo}</td></tr>
          <tr><td style="padding:8px 32px 0 32px;font:400 16px/1.5 system-ui,-apple-system,Segoe UI,sans-serif;color:#18181b">
            <p style="margin:0 0 16px 0">{Esc(greeting)}</p>
            <p style="margin:0 0 20px 0;color:#3f3f46">{Esc(intro)}</p>
          </td></tr>
          {quote}
          <tr><td align="center" style="padding:8px 32px 4px 32px">
            <a href="{Esc(portalUrl)}" style="display:inline-block;background:{color};color:#ffffff;text-decoration:none;font:600 16px system-ui,sans-serif;padding:14px 28px;border-radius:8px">{Esc(button)}</a>
          </td></tr>
          <tr><td style="padding:20px 32px 28px 32px;font:400 13px/1.5 system-ui,sans-serif;color:#71717a">
            <p style="margin:0">{Esc(closing)}</p>
          </td></tr>
        </table>
        <p style="max-width:560px;margin:16px auto 0;font:400 12px/1.5 system-ui,sans-serif;color:#a1a1aa;text-align:center">
          Recibís este mensaje porque empezaste una cotización en el portal de {Esc(branding.CompanyName)}.
        </p>
        </td></tr></table>
        </body></html>
        """;
    }

    /// <summary>
    /// The quote itself, shown only when there is one. A block reading "producto: —" is
    /// worse than no block: it advertises that we lost track of what they were buying.
    /// </summary>
    private static string BuildQuoteBlock(Lead lead, string color)
    {
        var product = lead.Steps.Step3.ProductName;
        var price = FormatPrice(lead.Steps.Step3.Amount);
        if (string.IsNullOrWhiteSpace(product) || string.IsNullOrWhiteSpace(price))
            return string.Empty;

        var vehicle = lead.Steps.Step1.VehicleTitle ?? lead.Steps.Step1.Plate ?? string.Empty;
        var vehicleRow = string.IsNullOrWhiteSpace(vehicle)
            ? string.Empty
            : $"<div style=\"font:400 13px system-ui,sans-serif;color:#71717a;margin-top:4px\">{Esc(vehicle)}</div>";

        return $"""
          <tr><td style="padding:0 32px 20px 32px">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e4e4e7;border-radius:10px">
              <tr><td style="padding:16px 18px">
                <div style="font:600 15px system-ui,sans-serif;color:#18181b">{Esc(product)}</div>
                {vehicleRow}
                <div style="font:700 22px system-ui,sans-serif;color:{color};margin-top:10px">{Esc(price)}</div>
              </td></tr>
            </table>
          </td></tr>
        """;
    }

    private static string BuildText(
        string greeting, string intro, string closing, string portalUrl, RecoveryBranding branding) =>
        $"""
        {greeting}

        {intro}

        Retomá tu compra: {portalUrl}

        {closing}

        —
        Recibís este mensaje porque empezaste una cotización en el portal de {branding.CompanyName}.
        """;

    /// <summary>
    /// Everything interpolated into the markup goes through here. The copy is tenant
    /// configuration and the vehicle and holder names come from whatever a visitor typed
    /// into the wizard, so none of it can be trusted to be inert HTML.
    /// </summary>
    private static string Esc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
