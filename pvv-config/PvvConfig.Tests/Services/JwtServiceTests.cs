using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using PvvConfig.Domain.Entities;
using PvvConfig.Domain.Enums;
using PvvConfig.Infrastructure.Services;
using PvvConfig.Infrastructure.Settings;

namespace PvvConfig.Tests.Services;

/// <summary>
/// The token is the only thing two other services trust. pvv-admin reads its role to
/// decide what to show, and pvv-bff scopes every leads query to the signed
/// <c>companyToken</c> claim — an operator who could widen that claim would be reading
/// another insurer's customers.
/// </summary>
public class JwtServiceTests
{
    private const string Secret = "a-test-signing-secret-long-enough-for-hmac-sha256";
    private const string CompanyToken = "cHZ2RGV2SXYxNkJ5dGVz_example_hash";

    [Fact]
    public void The_token_identifies_the_operator_and_its_role()
    {
        var op = OperatorFor(OperatorRole.CompanyOperator);

        var claims = ClaimsOf(Service().GenerateToken(op, CompanyToken).Token);

        Assert.Equal(op.OperatorId.ToString(), claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal("CompanyOperator", claims[ClaimTypes.Role]);
        Assert.Equal(op.Username, claims["username"]);
    }

    [Fact]
    public void A_company_operator_carries_the_company_it_manages()
    {
        var op = OperatorFor(OperatorRole.CompanyOperator);

        var claims = ClaimsOf(Service().GenerateToken(op, CompanyToken).Token);

        Assert.Equal(op.CompanyId!.Value.ToString(), claims["companyId"]);
    }

    [Fact]
    public void A_system_admin_belongs_to_no_company()
    {
        // The superadmin is platform-only: it administers companies and their
        // operators, and has no tenant data of its own to read.
        var op = OperatorFor(OperatorRole.SystemAdmin, companyId: null);

        var claims = ClaimsOf(Service().GenerateToken(op, companyToken: null).Token);

        Assert.Equal("SystemAdmin", claims[ClaimTypes.Role]);
        Assert.False(claims.ContainsKey("companyId"));
        Assert.False(claims.ContainsKey("companyToken"));
    }

    [Fact]
    public void The_portal_hash_travels_signed_so_the_bff_can_authorize_without_calling_back()
    {
        // pvv-bff cannot derive this itself: the AES key lives here.
        var claims = ClaimsOf(Service().GenerateToken(OperatorFor(), CompanyToken).Token);

        Assert.Equal(CompanyToken, claims["companyToken"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Without_a_resolvable_company_the_claim_is_omitted_rather_than_sent_empty(string? companyToken)
    {
        // An empty claim would be a scope that matches nothing — or, read carelessly
        // downstream, one that matches everything.
        var claims = ClaimsOf(Service().GenerateToken(OperatorFor(), companyToken).Token);

        Assert.False(claims.ContainsKey("companyToken"));
    }

    // ---- Signature and lifetime ------------------------------------------------

    [Fact]
    public void The_token_is_signed_with_the_configured_secret()
    {
        var token = Service().GenerateToken(OperatorFor(), CompanyToken).Token;

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("HS256", parsed.Header.Alg);
        Assert.Equal("pvv-config", parsed.Issuer);
        Assert.Contains("pvv", parsed.Audiences);
    }

    [Fact]
    public void An_empty_audience_is_left_out_instead_of_being_sent_blank()
    {
        var service = Service(new JwtSettings { Secret = Secret, Issuer = "pvv-config", Audience = "" });

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(
            service.GenerateToken(OperatorFor(), CompanyToken).Token);

        Assert.Empty(parsed.Audiences);
    }

    [Fact]
    public void A_session_lasts_eight_hours()
    {
        var (token, expiresAt) = Service().GenerateToken(OperatorFor(), CompanyToken);

        Assert.Equal(8, Math.Round((expiresAt - DateTime.UtcNow).TotalHours));
        // The reported expiry must match the one inside the token, or the panel
        // would keep sending a token the API has already stopped accepting.
        Assert.Equal(
            expiresAt.ToString("yyyy-MM-dd HH:mm:ss"),
            new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    // ---- Helpers ---------------------------------------------------------------

    private static JwtService Service(JwtSettings? settings = null) =>
        new(Options.Create(settings ?? new JwtSettings
        {
            Secret = Secret,
            Issuer = "pvv-config",
            Audience = "pvv",
        }));

    private static Operator OperatorFor(
        OperatorRole role = OperatorRole.CompanyOperator,
        Guid? companyId = null) => new()
        {
            OperatorId = Guid.NewGuid(),
            CompanyId = role == OperatorRole.SystemAdmin
                ? companyId
                : companyId ?? Guid.NewGuid(),
            Username = "admin@segucor.com",
            Role = role,
        };

    private static Dictionary<string, string> ClaimsOf(string token) =>
        new JwtSecurityTokenHandler()
            .ReadJwtToken(token).Claims
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.First().Value);
}
