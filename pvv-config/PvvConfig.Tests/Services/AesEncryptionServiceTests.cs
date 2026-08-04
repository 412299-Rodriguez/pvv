using Microsoft.Extensions.Options;
using PvvConfig.Infrastructure.Services;
using PvvConfig.Infrastructure.Settings;

namespace PvvConfig.Tests.Services;

/// <summary>
/// This encrypts a company's id into the token that appears in its public portal URL
/// (<c>?c=…</c>). Determinism is the whole point and the reason the IV is fixed: an
/// insurer publishes that link, so a token that changed on every encryption — the
/// normal, more secure behaviour for CBC — would break every link already in the wild.
/// The trade is deliberate and documented; these tests pin it so nobody "fixes" it
/// back into a random IV without meaning to.
/// </summary>
public class AesEncryptionServiceTests
{
    private const string Key = "pvv_test_aes_key_32_bytes_exact!";
    private const string Iv = "pvvTestIv16Byte!";

    [Fact]
    public void Encrypting_the_same_company_twice_produces_the_same_token()
    {
        var service = Service();
        var companyId = "11111111-1111-1111-1111-111111111111";

        Assert.Equal(service.Encrypt(companyId), service.Encrypt(companyId));
    }

    [Fact]
    public void A_token_survives_a_restart_because_the_key_and_iv_come_from_configuration()
    {
        // Two separate instances stand in for two runs of the service.
        var companyId = "11111111-1111-1111-1111-111111111111";

        Assert.Equal(Service().Encrypt(companyId), Service().Encrypt(companyId));
    }

    [Fact]
    public void What_was_encrypted_can_be_read_back()
    {
        var service = Service();
        var companyId = Guid.NewGuid().ToString();

        Assert.Equal(companyId, service.Decrypt(service.Encrypt(companyId)));
    }

    [Fact]
    public void Different_companies_get_different_tokens()
    {
        var service = Service();

        Assert.NotEqual(
            service.Encrypt("11111111-1111-1111-1111-111111111111"),
            service.Encrypt("22222222-2222-2222-2222-222222222222"));
    }

    [Fact]
    public void A_token_can_be_pasted_into_a_url_without_being_escaped()
    {
        // It travels as a query-string value in a link an insurer publishes.
        var token = Service().Encrypt(Guid.NewGuid().ToString());

        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public void A_token_produced_under_one_key_is_unreadable_under_another()
    {
        var token = Service().Encrypt("11111111-1111-1111-1111-111111111111");
        var otherTenantOfTheSameCode = Service(key: "another_test_key_32_bytes_exact!");

        Assert.ThrowsAny<Exception>(() => otherTenantOfTheSameCode.Decrypt(token));
    }

    // ---- Configuration guards --------------------------------------------------

    [Theory]
    [InlineData("too-short")]
    [InlineData("this_key_is_far_longer_than_thirty_two_bytes")]
    [InlineData("")]
    public void A_key_that_is_not_256_bits_fails_at_startup_rather_than_at_the_first_request(string key)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Service(key: key));

        Assert.Contains("32 bytes", error.Message);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("this_iv_is_much_longer_than_sixteen")]
    [InlineData("")]
    public void An_iv_that_is_not_128_bits_fails_at_startup(string iv)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Service(iv: iv));

        Assert.Contains("16 bytes", error.Message);
    }

    private static AesEncryptionService Service(string key = Key, string iv = Iv) =>
        new(Options.Create(new EncryptionSettings { Key = key, Iv = iv }));
}
