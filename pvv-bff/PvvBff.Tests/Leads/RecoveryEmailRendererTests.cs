using PvvBff.Application.Leads.Recovery;
using PvvBff.Domain.Leads;

namespace PvvBff.Tests.Leads;

/// <summary>
/// This is the only place in the system that builds markup out of data two different
/// untrusted parties supplied: the visitor typed the holder and vehicle details into
/// the wizard, and the operator wrote the template copy. Both end up inside an HTML
/// document that leaves the platform under our verified sender address.
/// </summary>
public class RecoveryEmailRendererTests
{
    private const string PortalUrl = "https://portal.test/?c=company-token";

    // ---- Placeholders ----------------------------------------------------------

    [Fact]
    public void The_greeting_uses_only_the_first_name()
    {
        // A recovery email opening with somebody's full legal name reads like a
        // debt collection notice.
        var lead = LeadWith(firstName: "Juan Carlos", lastName: "Perez");

        var message = Render(lead, new RecoveryEmailTemplate { Greeting = "Hola {nombre}," });

        Assert.Contains("Hola Juan,", message.HtmlBody);
        Assert.DoesNotContain("Carlos", message.HtmlBody);
    }

    [Fact]
    public void A_lead_that_never_gave_a_name_does_not_get_a_dangling_comma()
    {
        var lead = LeadWith(firstName: null);

        var message = Render(lead, new RecoveryEmailTemplate { Greeting = "Hola {nombre}," });

        Assert.Contains("Hola,", message.HtmlBody);
        Assert.DoesNotContain("Hola ,", message.HtmlBody);
    }

    [Fact]
    public void Every_placeholder_is_filled_from_what_the_lead_told_us()
    {
        var lead = LeadWith(
            firstName: "Juan", plate: "AA001BB", vehicleTitle: "Toyota Etios 2021",
            productName: "SOAT Full", amount: 28000m);

        var message = Render(lead, new RecoveryEmailTemplate
        {
            Subject = "{compania}: {producto} para tu {vehiculo}",
            Intro = "Patente {patente}, precio {precio}.",
        });

        Assert.Equal("SeguCor: SOAT Full para tu Toyota Etios 2021", message.Subject);
        Assert.Contains("Patente AA001BB", message.HtmlBody);
        Assert.Contains("28.000", message.HtmlBody);
    }

    [Fact]
    public void Without_a_vehicle_title_the_plate_stands_in_for_it()
    {
        var lead = LeadWith(plate: "AA001BB", vehicleTitle: null);

        var message = Render(lead, new RecoveryEmailTemplate { Intro = "Tu {vehiculo}" });

        // Asserted on the text body: the HTML one escapes accents into entities.
        Assert.Contains("vehículo AA001BB", message.TextBody);
    }

    [Fact]
    public void With_neither_title_nor_plate_the_vehicle_is_described_generically()
    {
        var lead = LeadWith(plate: null, vehicleTitle: null);

        var message = Render(lead, new RecoveryEmailTemplate { Intro = "Tu {vehiculo}" });

        Assert.Contains("vehículo", message.TextBody);
    }

    // ---- Injection -------------------------------------------------------------

    [Fact]
    public void Markup_typed_into_the_wizard_is_escaped_rather_than_rendered()
    {
        // The holder name is whatever a visitor typed. Interpolated raw, it would run
        // in whichever mail client is lenient enough to allow it.
        var lead = LeadWith(firstName: "<script>alert(1)</script>");

        var message = Render(lead, new RecoveryEmailTemplate { Greeting = "Hola {nombre}," });

        Assert.DoesNotContain("<script>", message.HtmlBody);
        Assert.Contains("&lt;script&gt;", message.HtmlBody);
    }

    [Fact]
    public void Markup_written_into_the_template_by_an_operator_is_escaped_too()
    {
        // The operator is trusted to write copy, not to inject markup into every
        // customer's inbox.
        var lead = LeadWith();

        var message = Render(lead, new RecoveryEmailTemplate
        {
            Intro = "<img src=x onerror=alert(1)>",
        });

        Assert.DoesNotContain("<img src=x", message.HtmlBody);
        Assert.Contains("&lt;img", message.HtmlBody);
    }

    [Fact]
    public void A_hostile_logo_url_cannot_break_out_of_its_attribute()
    {
        var lead = LeadWith();

        var message = Render(lead, new RecoveryEmailTemplate(),
            branding: new RecoveryBranding("SeguCor", "https://x.test/l.png\" onerror=\"alert(1)", "#0071ce"));

        Assert.DoesNotContain("onerror=\"alert(1)\"", message.HtmlBody);
    }

    // ---- Colour ----------------------------------------------------------------

    [Theory]
    [InlineData("#abc")]
    [InlineData("#0071ce")]
    [InlineData("#FFFFFF")]
    public void A_real_hex_colour_is_used_as_the_tenant_configured_it(string color)
    {
        var message = Render(LeadWith(), new RecoveryEmailTemplate(),
            branding: new RecoveryBranding("SeguCor", null, color));

        Assert.Contains($"background:{color}", message.HtmlBody);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#gggggg")]
    [InlineData("")]
    [InlineData("#0071ce;background-image:url(http://evil.test/x)")]
    public void Anything_that_is_not_a_hex_colour_falls_back_instead_of_reaching_the_style(string color)
    {
        // The colour is interpolated straight into a style attribute, so it is
        // allowed to be a hex colour and nothing else.
        var message = Render(LeadWith(), new RecoveryEmailTemplate(),
            branding: new RecoveryBranding("SeguCor", null, color));

        Assert.Contains("background:#0071ce", message.HtmlBody);
        Assert.DoesNotContain("evil.test", message.HtmlBody);
    }

    // ---- The quote block -------------------------------------------------------

    [Fact]
    public void The_quote_is_shown_when_the_lead_got_as_far_as_choosing_one()
    {
        var lead = LeadWith(productName: "SOAT Full", amount: 28000m, vehicleTitle: "Toyota Etios 2021");

        var message = Render(lead, new RecoveryEmailTemplate());

        Assert.Contains("SOAT Full", message.HtmlBody);
        Assert.Contains("Toyota Etios 2021", message.HtmlBody);
    }

    [Fact]
    public void A_lead_that_never_reached_a_price_gets_no_quote_block_at_all()
    {
        // "producto: —" is worse than nothing: it advertises that we lost track of
        // what they were buying.
        var lead = LeadWith(productName: null, amount: null);

        var message = Render(lead, new RecoveryEmailTemplate());

        Assert.DoesNotContain("border:1px solid #e4e4e7", message.HtmlBody);
    }

    // ---- Envelope and plain text -----------------------------------------------

    [Fact]
    public void The_message_is_addressed_to_the_contact_the_lead_left()
    {
        var lead = LeadWith(firstName: "Juan", lastName: "Perez");
        lead.ContactEmail = "juan@example.com";

        var message = Render(lead, new RecoveryEmailTemplate());

        Assert.Equal("juan@example.com", message.To);
        Assert.Equal("Juan Perez", message.ToName);
    }

    [Fact]
    public void Without_a_name_there_is_no_display_name_to_address()
    {
        var lead = LeadWith(firstName: null, lastName: null);

        var message = Render(lead, new RecoveryEmailTemplate());

        Assert.Null(message.ToName);
    }

    [Fact]
    public void A_plain_text_alternative_carries_the_portal_link_too()
    {
        // Clients that refuse HTML — and spam filters, which distrust a message with
        // only one body — read this one.
        var message = Render(LeadWith(), new RecoveryEmailTemplate());

        Assert.Contains(PortalUrl, message.TextBody);
        Assert.Contains(PortalUrl, message.HtmlBody);
        Assert.NotEmpty(message.TextBody);
    }

    // ---- Template merging ------------------------------------------------------

    [Fact]
    public void A_company_that_configured_nothing_still_gets_a_complete_email()
    {
        var merged = RecoveryEmailTemplate.Merge(null);

        Assert.NotEmpty(merged.Subject);
        Assert.NotEmpty(merged.Greeting);
        Assert.NotEmpty(merged.Intro);
        Assert.NotEmpty(merged.ButtonLabel);
        Assert.NotEmpty(merged.Closing);
    }

    [Fact]
    public void Customising_one_field_leaves_the_others_at_their_defaults()
    {
        // An operator who cleared a box to retype it and then saved must not be able
        // to send a message with a hole in the middle.
        var defaults = new RecoveryEmailTemplate();

        var merged = RecoveryEmailTemplate.Merge(new RecoveryEmailTemplate
        {
            Subject = "  Volvé a cotizar  ",
            Greeting = "   ",
        });

        Assert.Equal("Volvé a cotizar", merged.Subject);
        Assert.Equal(defaults.Greeting, merged.Greeting);
        Assert.Equal(defaults.Intro, merged.Intro);
    }

    // ---- Helpers ---------------------------------------------------------------

    private static Lead LeadWith(
        string? firstName = "Juan",
        string? lastName = "Perez",
        string? plate = "AA001BB",
        string? vehicleTitle = "Toyota Etios 2021",
        string? productName = null,
        decimal? amount = null)
    {
        var lead = new Lead { Id = "flow-1", ContactEmail = "juan@example.com" };
        lead.Steps.Step1.Plate = plate;
        lead.Steps.Step1.VehicleTitle = vehicleTitle;
        lead.Steps.Step2.FirstName = firstName;
        lead.Steps.Step2.LastName = lastName;
        lead.Steps.Step3.ProductName = productName;
        lead.Steps.Step3.Amount = amount;
        return lead;
    }

    private static Application.Abstractions.EmailMessage Render(
        Lead lead,
        RecoveryEmailTemplate template,
        RecoveryBranding? branding = null) =>
        RecoveryEmailRenderer.Render(
            lead,
            template,
            branding ?? new RecoveryBranding("SeguCor", null, "#0071ce"),
            PortalUrl);
}
