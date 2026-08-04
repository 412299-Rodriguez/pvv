using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using PvvBff.Application.Abstractions;

namespace PvvBff.Application.Leads.Recovery;

/// <summary>Sends the recovery email for one abandoned lead.</summary>
/// <param name="CompanyToken">
/// Taken from the operator's signed claim, never from the request. It scopes the lookup
/// itself, so there is no path that reaches another tenant's lead.
/// </param>
public sealed record RecoverLeadCommand(
    string FlowId,
    string CompanyToken,
    string OperatorName) : IRequest<RecoverLeadResult>;

/// <param name="Status">
/// sent · not_found · no_contact · already_recovered · send_failed. The caller maps
/// these to HTTP; the handler does not know about status codes.
/// </param>
public sealed record RecoverLeadResult(bool Sent, string Status, string? Error = null);

public sealed class RecoverLeadHandler : IRequestHandler<RecoverLeadCommand, RecoverLeadResult>
{
    private const string UiConfigType = "PVV_UI_CONFIG";
    private const string RecoveryConfigType = "RECOVERY_EMAIL_CONFIG";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ILeadRecoveryStore _store;
    private readonly ICompanyConfigReader _config;
    private readonly IEmailSender _email;
    private readonly IPortalLinkBuilder _links;
    private readonly ILogger<RecoverLeadHandler> _logger;

    public RecoverLeadHandler(
        ILeadRecoveryStore store,
        ICompanyConfigReader config,
        IEmailSender email,
        IPortalLinkBuilder links,
        ILogger<RecoverLeadHandler> logger)
    {
        _store = store;
        _config = config;
        _email = email;
        _links = links;
        _logger = logger;
    }

    public async Task<RecoverLeadResult> Handle(RecoverLeadCommand request, CancellationToken ct)
    {
        var lead = await _store.GetScopedAsync(request.FlowId, request.CompanyToken, ct);
        if (lead is null)
            return new RecoverLeadResult(false, "not_found");

        // The rule that defines this feature's scope: no address, no recovery. It is
        // enforced here and not only in the panel, because the panel is a convenience
        // and this is the boundary.
        if (string.IsNullOrWhiteSpace(lead.ContactEmail))
            return new RecoverLeadResult(false, "no_contact");

        // Claim BEFORE sending. Two operators pressing the button at the same instant is
        // the ordinary case this guards against, and the cost of losing that race is a
        // customer receiving the same email twice.
        var claimedAt = DateTime.UtcNow;
        if (!await _store.TryClaimRecoveryAsync(request.FlowId, claimedAt, request.OperatorName, ct))
            return new RecoverLeadResult(false, "already_recovered");

        var template = RecoveryEmailTemplate.Merge(
            await ReadAsync<RecoveryEmailTemplate>(request.CompanyToken, RecoveryConfigType, ct));
        var branding = await BuildBrandingAsync(request.CompanyToken, ct);
        var portalUrl = _links.BuildPortalUrl(request.CompanyToken);

        var message = RecoveryEmailRenderer.Render(lead, template, branding, portalUrl);
        var result = await _email.SendAsync(message, ct);

        if (!result.Sent)
        {
            // Nothing left, so nothing happened: hand the claim back or this lead stays
            // marked as contacted by an email that does not exist.
            await _store.ReleaseRecoveryClaimAsync(request.FlowId, ct);
            _logger.LogWarning("Recovery email for lead {FlowId} was not sent: {Error}",
                request.FlowId, result.Error);
            return new RecoverLeadResult(false, "send_failed", result.Error);
        }

        _logger.LogInformation("Recovery email sent for lead {FlowId} by {Operator}",
            request.FlowId, request.OperatorName);
        return new RecoverLeadResult(true, "sent");
    }

    /// <summary>
    /// Branding for the message. A company that never customised its portal still gets a
    /// usable email — the renderer falls back to its own defaults for anything missing.
    /// </summary>
    private async Task<RecoveryBranding> BuildBrandingAsync(string companyToken, CancellationToken ct)
    {
        var ui = await ReadAsync<UiBranding>(companyToken, UiConfigType, ct);

        return new RecoveryBranding(
            CompanyName: string.IsNullOrWhiteSpace(ui?.CompanyDisplayName)
                ? "tu aseguradora"
                : ui.CompanyDisplayName,
            LogoUrl: string.IsNullOrWhiteSpace(ui?.LogoUrl) ? null : ui.LogoUrl,
            PrimaryColor: ui?.PrimaryColor ?? string.Empty);
    }

    /// <summary>
    /// Same shape as QuoteHandler's reader: the config store hands back a boxed
    /// <see cref="JsonElement"/> on a cache hit and a plain object otherwise.
    /// </summary>
    private async Task<T?> ReadAsync<T>(string companyHash, string type, CancellationToken ct)
    {
        var raw = await _config.GetByHashAsync(companyHash, type, ct);
        return raw switch
        {
            null => default,
            JsonElement element => element.Deserialize<T>(JsonOptions),
            _ => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(raw), JsonOptions),
        };
    }

    /// <summary>Only the three fields of the UI blob the email actually uses.</summary>
    private sealed class UiBranding
    {
        public string? CompanyDisplayName { get; set; }
        public string? LogoUrl { get; set; }
        public string? PrimaryColor { get; set; }
    }
}
