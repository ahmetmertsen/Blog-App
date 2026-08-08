using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using buduns_server.Application.Common.Options;
using buduns_server.Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TokenHandler = buduns_server.Infrastructure.Services.Token.TokenHandler;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Access token'in icerigi kimlik dogrulama zincirinin tamamini belirliyor:
/// "sid" claim'i olmazsa oturum dogrulamasi calismaz, NameIdentifier olmazsa
/// CurrentUserBehavior kullaniciyi cozemez.
/// </summary>
public class TokenHandlerTests
{
    private const string SecurityKey = "unit-test-security-key-at-least-thirty-two-characters";

    [Fact]
    public void CreateAccessToken_ShouldIncludeIdentitySessionAndRoleClaims()
    {
        var sessionId = Guid.NewGuid();
        var token = CreateHandler().CreateAccessToken(CreateUser(emailConfirmed: true), new[] { "Admin", "User" }, sessionId, "refresh-token");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

        Assert.Equal("7", jwt.Claims.Single(claim => claim.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("7", jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("ahmet", jwt.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.Equal("ahmet@test.com", jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(sessionId.ToString(), jwt.Claims.Single(claim => claim.Type == "sid").Value);
        Assert.Equal(new[] { "Admin", "User" }, jwt.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value));
    }

    [Fact]
    public void CreateAccessToken_ShouldCarrySessionAndRefreshTokenOnResult()
    {
        var sessionId = Guid.NewGuid();

        var token = CreateHandler().CreateAccessToken(CreateUser(emailConfirmed: true), Array.Empty<string>(), sessionId, "refresh-token");

        Assert.Equal(sessionId, token.SessionId);
        Assert.Equal("refresh-token", token.RefreshToken);
        Assert.False(token.RequiresEmailVerification);
    }

    [Fact]
    public void CreateAccessToken_UnverifiedEmail_ShouldFlagVerificationRequirement()
    {
        var token = CreateHandler().CreateAccessToken(CreateUser(emailConfirmed: false), Array.Empty<string>(), Guid.NewGuid(), "refresh-token");

        Assert.True(token.RequiresEmailVerification);
    }

    [Fact]
    public void CreateAccessToken_ShouldUseConfiguredAudienceIssuerAndLifetime()
    {
        var token = CreateHandler(accessTokenExpirationMinutes: 30).CreateAccessToken(CreateUser(true), Array.Empty<string>(), Guid.NewGuid(), "refresh-token");
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

        Assert.Equal("buduns-tests", jwt.Issuer);
        Assert.Contains("buduns-audience", jwt.Audiences);
        Assert.Equal(token.Expiration, jwt.ValidTo, TimeSpan.FromSeconds(1));
        Assert.InRange(token.Expiration, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateAccessToken_NonPositiveLifetime_ShouldFallBackToFifteenMinutes(int accessTokenExpirationMinutes)
    {
        var token = CreateHandler(accessTokenExpirationMinutes).CreateAccessToken(CreateUser(true), Array.Empty<string>(), Guid.NewGuid(), "refresh-token");

        Assert.InRange(token.Expiration, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public void CreateAccessToken_ShouldBeVerifiableWithConfiguredSecurityKey()
    {
        var token = CreateHandler().CreateAccessToken(CreateUser(true), new[] { "User" }, Guid.NewGuid(), "refresh-token");

        var principal = new JwtSecurityTokenHandler().ValidateToken(token.AccessToken, new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = "buduns-audience",
            ValidIssuer = "buduns-tests",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        }, out _);

        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.True(principal.IsInRole("User"));
    }

    [Fact]
    public void CreateAccessToken_SignedWithDifferentKey_ShouldNotValidate()
    {
        var token = CreateHandler().CreateAccessToken(CreateUser(true), Array.Empty<string>(), Guid.NewGuid(), "refresh-token");

        Assert.ThrowsAny<SecurityTokenException>(() => new JwtSecurityTokenHandler().ValidateToken(token.AccessToken, new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("baska-bir-anahtar-en-az-otuz-iki-karakter-uzunlugunda"))
        }, out _));
    }

    [Fact]
    public void CreateAccessToken_ShouldProduceUniqueJtiPerToken()
    {
        var handler = CreateHandler();
        var first = new JwtSecurityTokenHandler().ReadJwtToken(handler.CreateAccessToken(CreateUser(true), Array.Empty<string>(), Guid.NewGuid(), "r").AccessToken);
        var second = new JwtSecurityTokenHandler().ReadJwtToken(handler.CreateAccessToken(CreateUser(true), Array.Empty<string>(), Guid.NewGuid(), "r").AccessToken);

        Assert.NotEqual(
            first.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value,
            second.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value);
    }

    [Fact]
    public void CreateAccessToken_NullUserNameAndEmail_ShouldProduceEmptyClaimsInsteadOfThrowing()
    {
        var user = new User { Id = 7, FullName = "Ahmet", UserName = null, Email = null, EmailConfirmed = true };

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(CreateHandler().CreateAccessToken(user, Array.Empty<string>(), Guid.NewGuid(), "r").AccessToken);

        Assert.Equal(string.Empty, jwt.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.Equal(string.Empty, jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
    }

    [Fact]
    public void CreateRefreshToken_ShouldProduce32ByteRandomBase64Values()
    {
        var handler = CreateHandler();
        var tokens = Enumerable.Range(0, 50).Select(_ => handler.CreateRefreshToken()).ToArray();

        Assert.All(tokens, token => Assert.Equal(32, Convert.FromBase64String(token).Length));
        Assert.Equal(tokens.Length, tokens.Distinct().Count());
    }

    private static TokenHandler CreateHandler(int accessTokenExpirationMinutes = 15) => new(Options.Create(new JwtTokenOptions
    {
        Audience = "buduns-audience",
        Issuer = "buduns-tests",
        SecurityKey = SecurityKey,
        AccessTokenExpirationMinutes = accessTokenExpirationMinutes,
        RefreshTokenExpirationDays = 30
    }));

    private static User CreateUser(bool emailConfirmed) => new()
    {
        Id = 7,
        UserName = "ahmet",
        FullName = "Ahmet Mert",
        Email = "ahmet@test.com",
        EmailConfirmed = emailConfirmed
    };
}
