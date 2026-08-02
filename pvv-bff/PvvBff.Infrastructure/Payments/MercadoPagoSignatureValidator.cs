using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PvvBff.Application.Abstractions;

namespace PvvBff.Infrastructure.Payments;

/// <summary>
/// Verifies Mercado Pago's <c>x-signature</c> header.
///
/// The header looks like <c>ts=1704908010,v1=618c8534...</c>. The signed manifest is
/// <c>id:{data.id};request-id:{x-request-id};ts:{ts};</c> — HMAC-SHA256 with the
/// application's webhook secret, hex-encoded — and a segment is omitted entirely when
/// its value is absent, which is why this builds the string piece by piece instead of
/// interpolating one template.
/// </summary>
public sealed class MercadoPagoSignatureValidator : IPaymentWebhookVerifier
{
    /// <summary>
    /// How stale a signature may be. Bounds replay: a captured notification stops being
    /// usable, without being so tight that ordinary clock drift rejects real traffic.
    /// </summary>
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

    private readonly MercadoPagoOptions _options;

    public MercadoPagoSignatureValidator(IOptions<MercadoPagoOptions> options) =>
        _options = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.WebhookSecret);

    public bool Verify(string providerPaymentId, string? signatureHeader, string? requestId)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(providerPaymentId))
            return false;

        if (!TryReadHeader(signatureHeader, out var timestamp, out var signature))
            return false;

        if (!IsFresh(timestamp))
            return false;

        var manifest = BuildManifest(providerPaymentId, requestId, timestamp);
        var expected = Sign(manifest, _options.WebhookSecret);

        // Constant-time: a byte-by-byte comparison that returns early leaks how much of
        // a guessed signature was correct, which is enough to forge one.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signature));
    }

    private static bool TryReadHeader(string? header, out string timestamp, out string signature)
    {
        timestamp = string.Empty;
        signature = string.Empty;

        if (string.IsNullOrWhiteSpace(header))
            return false;

        foreach (var part in header.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();

            if (key.Equals("ts", StringComparison.OrdinalIgnoreCase))
                timestamp = value;
            else if (key.Equals("v1", StringComparison.OrdinalIgnoreCase))
                signature = value;
        }

        return timestamp.Length > 0 && signature.Length > 0;
    }

    private static bool IsFresh(string timestamp)
    {
        if (!long.TryParse(timestamp, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixSeconds))
            return false;

        var age = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        // Both directions: a timestamp in the future is as suspect as an old one.
        return age <= MaxAge && age >= -MaxAge;
    }

    private static string BuildManifest(string paymentId, string? requestId, string timestamp)
    {
        // Mercado Pago lowercases an alphanumeric data.id before signing. Payment ids are
        // numeric today, so this only matters if that ever changes.
        var manifest = new StringBuilder()
            .Append("id:").Append(paymentId.ToLowerInvariant()).Append(';');

        if (!string.IsNullOrWhiteSpace(requestId))
            manifest.Append("request-id:").Append(requestId).Append(';');

        return manifest.Append("ts:").Append(timestamp).Append(';').ToString();
    }

    private static string Sign(string manifest, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
        return Convert.ToHexStringLower(hash);
    }
}
