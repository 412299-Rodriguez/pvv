using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PvvBff.Application.Abstractions;

namespace PvvBff.Infrastructure.Payments;

/// <summary>
/// Real Mercado Pago gateway — Checkout Pro in its redirect flow.
///
/// Redirect rather than Bricks/Checkout API on purpose: the card is entered on
/// Mercado Pago's own domain, so no card data ever reaches our servers and the
/// frontend needs neither the public key nor their JS SDK.
///
/// A typed HttpClient over the REST API rather than the official SDK, also on
/// purpose: we need three endpoints in total, and the SDK takes its credentials
/// from a static global, which fights dependency injection and would stand in
/// the way of a per-tenant token later on.
/// </summary>
public sealed class MercadoPagoGateway : IPaymentGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly MercadoPagoOptions _options;

    public MercadoPagoGateway(HttpClient http, IOptions<MercadoPagoOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<PaymentPreferenceResult> CreatePreferenceAsync(
        PaymentPreferenceRequest request, CancellationToken ct)
    {
        // One URL for all three outcomes. The result page does not trust what
        // Mercado Pago appends to it anyway — it re-asks our own backend for the
        // real state — so there is nothing to gain from three separate landings.
        var backUrl = BuildBackUrl(request.TransactionId);

        var body = new PreferenceRequest
        {
            Items =
            [
                new PreferenceItem
                {
                    Title = request.Description,
                    Quantity = 1,
                    UnitPrice = request.Amount,
                    CurrencyId = _options.CurrencyId,
                },
            ],
            // The ONLY way back from a Mercado Pago payment to our transaction:
            // their notification carries their payment id, not ours.
            ExternalReference = request.TransactionId,
            BackUrls = new PreferenceBackUrls
            {
                Success = backUrl,
                Failure = backUrl,
                Pending = backUrl,
            },
            AutoReturn = _options.AutoReturn ? "approved" : null,
            NotificationUrl = string.IsNullOrWhiteSpace(_options.NotificationUrl)
                ? null
                : _options.NotificationUrl,
            Expires = true,
            ExpirationDateTo = FormatExpiry(DateTimeOffset.Now.AddMinutes(_options.PreferenceExpiryMinutes)),
            StatementDescriptor = _options.StatementDescriptor,
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, "/checkout/preferences")
        {
            Content = JsonContent.Create(body, options: JsonOptions),
        };
        Authorize(message);
        // Retrying a preference creation must not create a second checkout.
        message.Headers.Add("X-Idempotency-Key", request.TransactionId);

        using var response = await _http.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode)
        {
            // Mercado Pago explains its 400s in the body (which back_url it did
            // not like, which field is missing). Swallowing it would turn every
            // misconfiguration into an opaque failure.
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException(
                $"Mercado Pago rejected the preference ({(int)response.StatusCode}): {error}");
        }

        var dto = await response.Content.ReadFromJsonAsync<PreferenceResponse>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Mercado Pago returned an empty preference.");

        // The token belongs to a Mercado Pago TEST USER, so the whole account is
        // already a sandbox and init_point is the URL to send the buyer to.
        // sandbox_init_point is the older TEST- credential mode; kept only as a
        // fallback in case the account type changes.
        var initPoint = !string.IsNullOrWhiteSpace(dto.InitPoint) ? dto.InitPoint : dto.SandboxInitPoint;
        if (string.IsNullOrWhiteSpace(initPoint))
            throw new InvalidOperationException("Mercado Pago returned a preference with no init_point.");

        return new PaymentPreferenceResult(dto.Id, initPoint);
    }

    private void Authorize(HttpRequestMessage message) =>
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

    /// <summary>
    /// Where Mercado Pago returns the buyer. Carries the transaction id because
    /// the browser comes back with no session state worth trusting.
    /// </summary>
    private string BuildBackUrl(string transactionId) =>
        $"{_options.BackUrlBase.TrimEnd('/')}/?tx={Uri.EscapeDataString(transactionId)}";

    /// <summary>
    /// Mercado Pago wants ISO 8601 with milliseconds AND an explicit offset;
    /// a bare UTC "Z" timestamp is rejected.
    /// </summary>
    private static string FormatExpiry(DateTimeOffset at) => at.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");

    // ---- Wire contracts. Mercado Pago is snake_case, so every name is explicit
    // rather than relying on the serializer's convention. ------------------------

    private sealed class PreferenceRequest
    {
        [JsonPropertyName("items")]
        public PreferenceItem[] Items { get; set; } = [];

        [JsonPropertyName("external_reference")]
        public string ExternalReference { get; set; } = string.Empty;

        [JsonPropertyName("back_urls")]
        public PreferenceBackUrls? BackUrls { get; set; }

        [JsonPropertyName("auto_return")]
        public string? AutoReturn { get; set; }

        [JsonPropertyName("notification_url")]
        public string? NotificationUrl { get; set; }

        [JsonPropertyName("expires")]
        public bool Expires { get; set; }

        [JsonPropertyName("expiration_date_to")]
        public string? ExpirationDateTo { get; set; }

        [JsonPropertyName("statement_descriptor")]
        public string? StatementDescriptor { get; set; }
    }

    private sealed class PreferenceItem
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unit_price")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("currency_id")]
        public string CurrencyId { get; set; } = string.Empty;
    }

    private sealed class PreferenceBackUrls
    {
        [JsonPropertyName("success")]
        public string Success { get; set; } = string.Empty;

        [JsonPropertyName("failure")]
        public string Failure { get; set; } = string.Empty;

        [JsonPropertyName("pending")]
        public string Pending { get; set; } = string.Empty;
    }

    private sealed class PreferenceResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("init_point")]
        public string InitPoint { get; set; } = string.Empty;

        [JsonPropertyName("sandbox_init_point")]
        public string SandboxInitPoint { get; set; } = string.Empty;
    }
}
