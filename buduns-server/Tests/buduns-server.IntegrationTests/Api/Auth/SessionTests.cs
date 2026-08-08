using buduns_server.Application.Features.Auth.GetSessions;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Auth;

/// <summary>
/// Her istek JwtBearerEvents icinde oturumun hala acik oldugunu sorguluyor.
/// Oturum iptali bu yuzden yalnizca veritabani degil, canli erisim uzerinde de
/// dogrulanmali.
/// </summary>
public sealed class SessionTests : IntegrationTestBase
{
    public SessionTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Sessions_endpoint_should_list_active_sessions_and_flag_the_current_one()
    {
        var user = await CreateUserAsync("session-list-user");
        using var first = await Factory.CreateAuthenticatedClientAsync(user.Id);
        using var second = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await first.Client.GetFromJsonAsync<GetAuthSessionsQueryResponse>("/api/Auth/sessions");

        response!.Sessions.Should().HaveCount(2);
        response.Sessions.Count(session => session.IsCurrent).Should().Be(1);
        response.Sessions.Single(session => session.IsCurrent).Id.Should().Be(first.SessionId);
        response.Sessions.Should().Contain(session => session.Id == second.SessionId);
    }

    [Fact]
    public async Task Logout_should_revoke_only_the_current_session()
    {
        var user = await CreateUserAsync("logout-user");
        using var first = await Factory.CreateAuthenticatedClientAsync(user.Id);
        using var second = await Factory.CreateAuthenticatedClientAsync(user.Id);

        (await first.Client.PostAsync("/api/Auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await first.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await second.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task LogoutAll_should_revoke_every_session()
    {
        var user = await CreateUserAsync("logout-all-user");
        using var first = await Factory.CreateAuthenticatedClientAsync(user.Id);
        using var second = await Factory.CreateAuthenticatedClientAsync(user.Id);

        (await first.Client.PostAsync("/api/Auth/logoutAll", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await first.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await second.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Revoke_session_should_close_the_targeted_session()
    {
        var user = await CreateUserAsync("revoke-session-user");
        using var current = await Factory.CreateAuthenticatedClientAsync(user.Id);
        using var other = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await current.Client.DeleteAsync($"/api/Auth/sessions/{other.SessionId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await other.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await current.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Revoke_unknown_session_should_return_not_found()
    {
        var user = await CreateUserAsync("revoke-unknown-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.DeleteAsync($"/api/Auth/sessions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Revoke_session_of_another_user_should_return_not_found()
    {
        var owner = await CreateUserAsync("session-owner");
        var attacker = await CreateUserAsync("session-attacker");
        using var ownerAuthentication = await Factory.CreateAuthenticatedClientAsync(owner.Id);
        using var attackerAuthentication = await Factory.CreateAuthenticatedClientAsync(attacker.Id);

        var response = await attackerAuthentication.Client.DeleteAsync($"/api/Auth/sessions/{ownerAuthentication.SessionId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ownerAuthentication.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Revoked_session_should_not_be_listed_anymore()
    {
        var user = await CreateUserAsync("session-cleanup-user");
        using var current = await Factory.CreateAuthenticatedClientAsync(user.Id);
        using var other = await Factory.CreateAuthenticatedClientAsync(user.Id);

        await current.Client.DeleteAsync($"/api/Auth/sessions/{other.SessionId}");
        var response = await current.Client.GetFromJsonAsync<GetAuthSessionsQueryResponse>("/api/Auth/sessions");

        response!.Sessions.Should().ContainSingle().Which.Id.Should().Be(current.SessionId);

        var revoked = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().AuthSessions.AsNoTracking().SingleAsync(session => session.Id == other.SessionId));
        revoked.RevokedAt.Should().NotBeNull();
        revoked.RevokedReason.Should().Be("Revoked by user");
    }

    [Fact]
    public async Task Session_endpoints_should_reject_anonymous_requests()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsync("/api/Auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsync("/api/Auth/logoutAll", null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.DeleteAsync($"/api/Auth/sessions/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_token_should_produce_a_working_session_and_close_the_old_one()
    {
        var user = await CreateUserAsync("rotation-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);
        using var client = Factory.CreateHttpsClient();

        var refreshResponse = await client.PostAsJsonAsync("/api/Auth/refreshTokenLogin", new Application.Features.Auth.RefreshTokenLogin.RefreshTokenLoginCommand { RefreshToken = authentication.RefreshToken });
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<Application.Features.Auth.RefreshTokenLogin.RefreshTokenLoginCommandResponse>();

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        refreshed!.Token.SessionId.Should().NotBe(authentication.SessionId);

        using var rotatedClient = Factory.CreateHttpsClient();
        rotatedClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", refreshed.Token.AccessToken);

        (await rotatedClient.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.OK);
        // Eski access token'in oturumu artik kapali olmali.
        (await authentication.Client.GetAsync("/api/Auth/sessions")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_unknown_token_should_return_unauthorized()
    {
        using var client = Factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/Auth/refreshTokenLogin", new Application.Features.Auth.RefreshTokenLogin.RefreshTokenLoginCommand { RefreshToken = "bilinmeyen-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Banned_user_refresh_should_be_rejected_and_close_all_sessions()
    {
        var user = await CreateUserAsync("banned-refresh-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetUserStatusAsync(services, user.Id, Domain.Enums.UserStatus.Banned));
        using var client = Factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/Auth/refreshTokenLogin", new Application.Features.Auth.RefreshTokenLogin.RefreshTokenLoginCommand { RefreshToken = authentication.RefreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var activeSessionCount = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().AuthSessions.CountAsync(session => session.UserId == user.Id && session.RevokedAt == null));
        activeSessionCount.Should().Be(0);
    }
}
