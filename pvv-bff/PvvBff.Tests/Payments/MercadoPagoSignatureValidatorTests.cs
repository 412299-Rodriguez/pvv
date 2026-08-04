using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using PvvBff.Infrastructure.Payments;

namespace PvvBff.Tests.Payments;

/// <summary>
/// The webhook is the only endpoint that can turn a payment into an issued policy,
/// and it is anonymous by necessity — Mercado Pago cannot log in. The signature is
/// therefore the whole of its authentication: if this accepts a forged notification,
/// anyone who knows a transaction id can have a policy emitted for free.
///
/// The expected signature is recomputed here from Mercado Pago's published manifest
/// rather than borrowed from the implementation, so the test pins the contract with
/// the provider instead of agreeing with whatever the code happens to do.
/// </summary>
public class MercadoPagoSignatureValidatorTests
{
    private const string Secret = "a-webhook-secret";
    private const string PaymentId = "1234567890";
    private const string RequestId = "req-abc-123";

    [Fact]
    public void A_correctly_signed_notification_is_accepted()
    {
        var validator = ValidatorWith(Secret);
        var (header, _) = SignedHeader(PaymentId, RequestId, Now());

        Assert.True(validator.Verify(PaymentId, header, RequestId));
    }

    [Fact]
    public void Without_a_configured_secret_nothing_is_accepted()
    {
        // Fail closed: an unconfigured deployment must not verify by accident.
        var validator = ValidatorWith("");
        var (header, _) = SignedHeader(PaymentId, RequestId, Now());

        Assert.False(validator.IsConfigured);
        Assert.False(validator.Verify(PaymentId, header, RequestId));
    }

    // ---- Forgery ---------------------------------------------------------------

    [Fact]
    public void A_notification_signed_with_the_wrong_secret_is_rejected()
    {
        var validator = ValidatorWith(Secret);
        var (header, _) = SignedHeader(PaymentId, RequestId, Now(), secret: "someone-elses-secret");

        Assert.False(validator.Verify(PaymentId, header, RequestId));
    }

    [Fact]
    public void Swapping_the_payment_id_after_signing_is_rejected()
    {
        // The attack this exists to stop: replay a real notification pointing at a
        // different payment.
        var validator = ValidatorWith(Secret);
        var (header, _) = SignedHeader(PaymentId, RequestId, Now());

        Assert.False(validator.Verify("9999999999", header, RequestId));
    }

    [Fact]
    public void Changing_the_request_id_after_signing_is_rejected()
    {
        var validator = ValidatorWith(Secret);
        var (header, _) = SignedHeader(PaymentId, RequestId, Now());

        Assert.False(validator.Verify(PaymentId, header, "req-tampered"));
    }

    [Fact]
    public void A_truncated_signature_is_rejected()
    {
        // Guards the constant-time comparison against a length shortcut.
        var validator = ValidatorWith(Secret);
        var timestamp = Now();
        var (_, signature) = SignedHeader(PaymentId, RequestId, timestamp);

        Assert.False(validator.Verify(PaymentId, $"ts={timestamp},v1={signature[..20]}", RequestId));
    }

    // ---- Replay window ---------------------------------------------------------

    [Fact]
    public void A_notification_older_than_five_minutes_is_rejected()
    {
        var validator = ValidatorWith(Secret);
        var stale = DateTimeOffset.UtcNow.AddMinutes(-6).ToUnixTimeSeconds().ToString();
        var (header, _) = SignedHeader(PaymentId, RequestId, stale);

        Assert.False(validator.Verify(PaymentId, header, RequestId));
    }

    [Fact]
    public void A_notification_timestamped_in_the_future_is_equally_suspect()
    {
        var validator = ValidatorWith(Secret);
        var future = DateTimeOffset.UtcNow.AddMinutes(6).ToUnixTimeSeconds().ToString();
        var (header, _) = SignedHeader(PaymentId, RequestId, future);

        Assert.False(validator.Verify(PaymentId, header, RequestId));
    }

    [Fact]
    public void Ordinary_clock_drift_inside_the_window_still_verifies()
    {
        // Tight enough to bound replay, loose enough not to reject real traffic.
        var validator = ValidatorWith(Secret);
        var recent = DateTimeOffset.UtcNow.AddMinutes(-4).ToUnixTimeSeconds().ToString();
        var (header, _) = SignedHeader(PaymentId, RequestId, recent);

        Assert.True(validator.Verify(PaymentId, header, RequestId));
    }

    [Fact]
    public void A_non_numeric_timestamp_is_rejected()
    {
        var validator = ValidatorWith(Secret);

        Assert.False(validator.Verify(PaymentId, "ts=not-a-timestamp,v1=deadbeef", RequestId));
    }

    // ---- Header parsing --------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("garbage")]
    [InlineData("v1=onlysignature")]
    [InlineData("ts=1704908010")]
    public void A_header_that_is_missing_a_part_is_rejected(string? header)
    {
        var validator = ValidatorWith(Secret);

        Assert.False(validator.Verify(PaymentId, header, RequestId));
    }

    [Fact]
    public void An_empty_payment_id_is_rejected()
    {
        var validator = ValidatorWith(Secret);
        var (header, _) = SignedHeader(PaymentId, RequestId, Now());

        Assert.False(validator.Verify("", header, RequestId));
    }

    [Fact]
    public void The_header_parts_may_arrive_in_any_order_with_padding()
    {
        var validator = ValidatorWith(Secret);
        var timestamp = Now();
        var (_, signature) = SignedHeader(PaymentId, RequestId, timestamp);

        Assert.True(validator.Verify(PaymentId, $" v1={signature} , ts={timestamp} ", RequestId));
    }

    [Fact]
    public void The_part_names_are_matched_case_insensitively()
    {
        var validator = ValidatorWith(Secret);
        var timestamp = Now();
        var (_, signature) = SignedHeader(PaymentId, RequestId, timestamp);

        Assert.True(validator.Verify(PaymentId, $"TS={timestamp},V1={signature}", RequestId));
    }

    // ---- Manifest shape --------------------------------------------------------

    [Fact]
    public void Without_a_request_id_that_segment_is_left_out_of_the_manifest_entirely()
    {
        // Mercado Pago omits an absent segment rather than signing an empty one, so
        // a missing x-request-id has to change the manifest, not just its value.
        var validator = ValidatorWith(Secret);
        var timestamp = Now();
        var signature = Sign($"id:{PaymentId};ts:{timestamp};", Secret);

        Assert.True(validator.Verify(PaymentId, $"ts={timestamp},v1={signature}", requestId: null));
    }

    [Fact]
    public void An_alphanumeric_payment_id_is_lowercased_before_signing()
    {
        var validator = ValidatorWith(Secret);
        var timestamp = Now();
        var signature = Sign($"id:abc123;request-id:{RequestId};ts:{timestamp};", Secret);

        Assert.True(validator.Verify("ABC123", $"ts={timestamp},v1={signature}", RequestId));
    }

    // ---- Helpers ---------------------------------------------------------------

    private static MercadoPagoSignatureValidator ValidatorWith(string secret) =>
        new(Options.Create(new MercadoPagoOptions { WebhookSecret = secret }));

    private static string Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    private static (string Header, string Signature) SignedHeader(
        string paymentId, string? requestId, string timestamp, string secret = Secret)
    {
        var manifest = requestId is null
            ? $"id:{paymentId.ToLowerInvariant()};ts:{timestamp};"
            : $"id:{paymentId.ToLowerInvariant()};request-id:{requestId};ts:{timestamp};";

        var signature = Sign(manifest, secret);
        return ($"ts={timestamp},v1={signature}", signature);
    }

    private static string Sign(string manifest, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest)));
    }
}
