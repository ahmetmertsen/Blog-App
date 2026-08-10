using buduns_server.Application.Common.Consts;
using buduns_server.Application.Features.Auth.RefreshTokenLogin;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using buduns_server.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Auth;

public sealed class RefreshTokenTests : IntegrationTestBase
{
    public RefreshTokenTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Refresh_token_should_rotate_and_reuse_should_revoke_token_family()
    {
        var user = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, "refresh-user", "Refresh User", RoleConstants.User));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);
        using var client = Factory.CreateHttpsClient();

        var firstResponse = await client.PostAsJsonAsync("/api/Auth/refreshTokenLogin", new RefreshTokenLoginCommand { RefreshToken = authentication.RefreshToken });
        var firstBody = await firstResponse.ReadDataAsync<RefreshTokenLoginCommandResponse>();
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        firstBody!.Token.RefreshToken.Should().NotBe(authentication.RefreshToken);

        var reuseResponse = await client.PostAsJsonAsync("/api/Auth/refreshTokenLogin", new RefreshTokenLoginCommand { RefreshToken = authentication.RefreshToken });
        var reuseError = await reuseResponse.ReadErrorAsync();
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        reuseError.Code.Should().Be("INVALID_REFRESH_TOKEN");

        var activeSessionCount = await Factory.ExecuteScopeAsync(async services => await services.GetRequiredService<BudunsDbContext>().AuthSessions.CountAsync(session => session.UserId == user.Id && session.RevokedAt == null));
        activeSessionCount.Should().Be(0);
    }
}
