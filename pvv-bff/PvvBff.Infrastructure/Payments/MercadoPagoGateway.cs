using System.Net;
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
        var backUrl = BuildBackUrl(request.TransactionId, request.CompanyToken);

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

        var initPoint = SelectInitPoint(dto);
        if (string.IsNullOrWhiteSpace(initPoint))
            throw new InvalidOperationException("Mercado Pago returned a preference with no init_point.");

        return new PaymentPreferenceResult(dto.Id, initPoint);
    }

    public async Task<GatewayPayment?> GetPaymentAsync(string providerPaymentId, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Get, $"/v1/payments/{Uri.EscapeDataString(providerPaymentId)}");
        Authorize(message);

        using var response = await _http.SendAsync(message, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions, ct);
        return dto is null ? null : Map(dto);
    }

    public async Task<GatewayPayment?> FindPaymentByTransactionAsync(string transactionId, CancellationToken ct)
    {
        // Newest first, so results[0] is the most recent attempt.
        using var message = new HttpRequestMessage(
            HttpMethod.Get,
            $"/v1/payments/search?external_reference={Uri.EscapeDataString(transactionId)}" +
            "&sort=date_created&criteria=desc");
        Authorize(message);

        using var response = await _http.SendAsync(message, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PaymentSearchResponse>(JsonOptions, ct);
        if (dto?.Results is not { Length: > 0 } results)
            return null;

        // One preference can accumulate several payments: a buyer whose card is
        // rejected simply tries another one, and each attempt is its own payment.
        // An approval anywhere in that history is the outcome that matters —
        // otherwise the latest attempt is the one that describes the state.
        var payments = results.Select(Map).ToArray();
        return Array.Find(payments, p => p.Outcome == PaymentOutcome.Approved) ?? payments[0];
    }

    private static GatewayPayment Map(PaymentResponse dto) =>
        new(
            dto.Id.ToString(),
            dto.ExternalReference ?? string.Empty,
            MapOutcome(dto.Status),
            dto.Status ?? string.Empty);

    private static PaymentOutcome MapOutcome(string? status) => status switch
    {
        "approved" or "authorized" => PaymentOutcome.Approved,
        "rejected" or "cancelled" => PaymentOutcome.Rejected,
        // pending / in_process / in_mediation are still in flight. refunded and
        // charged_back reverse a payment whose policy was already issued, which is
        // out of scope for HU-11 — and must not be mistaken for a rejection, which
        // would be a wrong answer rather than an incomplete one.
        _ => PaymentOutcome.Pending,
    };

    /// <summary>
    /// Which checkout to send the buyer to.
    ///
    /// These are two different environments on two different hosts, not two
    /// spellings of one URL: init_point is production (www.mercadopago.com.ar) and
    /// sandbox_init_point is the test one (sandbox.mercadopago.com.ar). Landing a
    /// buyer on production while the collector is a test account is the mismatch
    /// Mercado Pago refuses with "una de las partes con la que intentás hacer el
    /// pago es de prueba" — and it refuses it regardless of who the buyer is, which
    /// makes it very easy to misread as a problem with the payer and go hunting on
    /// the wrong side.
    /// </summary>
    private string SelectInitPoint(PreferenceResponse dto)
    {
        var preferred = _options.UseSandbox ? dto.SandboxInitPoint : dto.InitPoint;
        var fallback = _options.UseSandbox ? dto.InitPoint : dto.SandboxInitPoint;
        return !string.IsNullOrWhiteSpace(preferred) ? preferred : fallback;
    }

    private void Authorize(HttpRequestMessage message) =>
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

    /// <summary>
    /// Where Mercado Pago returns the buyer. Carries the transaction id, because
    /// the browser comes back with no state worth trusting, and the company token,
    /// because Mercado Pago owns this redirect and the frontend cannot add it.
    /// </summary>
    private string BuildBackUrl(string transactionId, string? companyToken)
    {
        var url = $"{_options.BackUrlBase.TrimEnd('/')}/?tx={Uri.EscapeDataString(transactionId)}";
        return string.IsNullOrWhiteSpace(companyToken)
            ? url
            : $"{url}&c={Uri.EscapeDataString(companyToken)}";
    }

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

    private sealed class PaymentResponse
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>Our transaction id, set when the preference was created.</summary>
        [JsonPropertyName("external_reference")]
        public string? ExternalReference { get; set; }
    }

    private sealed class PaymentSearchResponse
    {
        [JsonPropertyName("results")]
        public PaymentResponse[]? Results { get; set; }
    }
}
