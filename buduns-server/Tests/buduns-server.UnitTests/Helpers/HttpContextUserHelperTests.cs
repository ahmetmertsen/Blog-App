using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using buduns_server.Application.Common.Helpers;
using Microsoft.AspNetCore.Http;

namespace buduns_server.UnitTests.Helpers;

/// <summary>
/// Anonim istekler icin de calisan sorgular (getAll, getById, tag) izleyicinin
/// kim oldugunu bu yardimci uzerinden ogreniyor. Yanlis bir kimlik cozumu
/// "begendim/kaydettim" bayraklarini baska kullaniciya gosterir.
/// </summary>
public class HttpContextUserHelperTests
{
    [Fact]
    public void GetUserId_NullContext_ShouldReturnNull()
    {
        Assert.Null(HttpContextUserHelper.GetUserId(null));
    }

    [Fact]
    public void GetUserId_AnonymousUser_ShouldReturnNull()
    {
        Assert.Null(HttpContextUserHelper.GetUserId(new DefaultHttpContext()));
    }

    [Fact]
    public void GetUserId_AuthenticatedWithNameIdentifier_ShouldReturnId()
    {
        var context = CreateAuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, "42"));

        Assert.Equal(42, HttpContextUserHelper.GetUserId(context));
    }

    [Fact]
    public void GetUserId_AuthenticatedWithSubClaimOnly_ShouldReturnId()
    {
        var context = CreateAuthenticatedContext(new Claim(JwtRegisteredClaimNames.Sub, "7"));

        Assert.Equal(7, HttpContextUserHelper.GetUserId(context));
    }

    [Fact]
    public void GetUserId_ShouldPreferNameIdentifierOverSub()
    {
        var context = CreateAuthenticatedContext(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(JwtRegisteredClaimNames.Sub, "7"));

        Assert.Equal(42, HttpContextUserHelper.GetUserId(context));
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("")]
    [InlineData("9999999999999999999")]
    public void GetUserId_UnparsableClaim_ShouldReturnNull(string claimValue)
    {
        var context = CreateAuthenticatedContext(new Claim(ClaimTypes.NameIdentifier, claimValue));

        Assert.Null(HttpContextUserHelper.GetUserId(context));
    }

    [Fact]
    public void GetUserId_AuthenticatedWithoutIdentifierClaim_ShouldReturnNull()
    {
        var context = CreateAuthenticatedContext(new Claim(ClaimTypes.Name, "ahmet"));

        Assert.Null(HttpContextUserHelper.GetUserId(context));
    }

    [Fact]
    public void GetUserId_UnauthenticatedIdentityWithClaim_ShouldReturnNull()
    {
        // Kimlik dogrulanmamis olsa bile claim tasiyabilir; kabul edilmemeli.
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") });
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        Assert.Null(HttpContextUserHelper.GetUserId(context));
    }

    private static DefaultHttpContext CreateAuthenticatedContext(params Claim[] claims) =>
        new() { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication")) };
}
