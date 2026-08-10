using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Users.Commands.Update.UpdateProfile;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using buduns_server.WebAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Users;

public sealed class UserProfileTests : IntegrationTestBase
{
    public UserProfileTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Update_profile_should_change_only_the_current_users_profile()
    {
        var user = await CreateUserAsync("profile-user");
        var other = await CreateUserAsync("profile-other");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/User/updateUserProfile", new UpdateUserProfileCommand
        {
            FullName = "Guncellenmis Ad",
            Bio = "yeni bio",
            ImageUrl = "https://cdn.example.com/avatar.png"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(item => item.Id == user.Id));
        stored.FullName.Should().Be("Guncellenmis Ad");
        stored.Bio.Should().Be("yeni bio");
        stored.ImageUrl.Should().Be("https://cdn.example.com/avatar.png");

        var untouched = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(item => item.Id == other.Id));
        untouched.FullName.Should().Be("profile other");
    }

    [Fact]
    public async Task Update_profile_should_clear_optional_fields_when_omitted()
    {
        var user = await CreateUserAsync("clear-profile-user");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);
        (await authentication.Client.PostAsJsonAsync("/api/User/updateUserProfile", new UpdateUserProfileCommand { FullName = "Ad", Bio = "bio", ImageUrl = "http://img" })).EnsureSuccessStatusCode();

        (await authentication.Client.PostAsJsonAsync("/api/User/updateUserProfile", new UpdateUserProfileCommand { FullName = "Ad" })).EnsureSuccessStatusCode();

        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Users.AsNoTracking().SingleAsync(item => item.Id == user.Id));
        stored.Bio.Should().BeNull();
        stored.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task Update_profile_validation_errors_should_follow_the_global_contract()
    {
        var user = await CreateUserAsync("invalid-profile-user");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/User/updateUserProfile", new UpdateUserProfileCommand
        {
            FullName = string.Empty,
            Bio = new string('b', 1001)
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.ReadErrorAsync();
        error.ValidationErrors.Should().ContainKey("FullName").And.ContainKey("Bio");
    }

    [Fact]
    public async Task Update_profile_should_reject_anonymous_requests()
    {
        using var client = Factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/User/updateUserProfile", new UpdateUserProfileCommand { FullName = "Ad" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Public_profile_lookups_should_work_by_id_and_username()
    {
        var user = await CreateUserAsync("public-profile-user");
        using var client = Factory.CreateHttpsClient();

        var byId = await client.GetDataAsync<UserDto>($"/api/User/getUserById/{user.Id}");
        var byUsername = await client.GetDataAsync<UserDto>("/api/User/getUserByUsername/public-profile-user");

        byId!.UserName.Should().Be("public-profile-user");
        byUsername!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Profile_lookup_by_username_should_be_case_insensitive()
    {
        var user = await CreateUserAsync("case-profile-user");
        using var client = Factory.CreateHttpsClient();

        var response = await client.GetDataAsync<UserDto>("/api/User/getUserByUsername/CASE-PROFILE-USER");

        response!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Missing_profile_lookups_should_return_not_found()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.GetAsync("/api/User/getUserById/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync("/api/User/getUserByUsername/olmayan-kullanici")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Profile_lookup_with_invalid_input_should_return_validation_error()
    {
        using var client = Factory.CreateHttpsClient();

        var byId = await client.GetAsync("/api/User/getUserById/0");
        var byUsername = await client.GetAsync("/api/User/getUserByUsername/ab");

        byId.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        byUsername.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_user_listing_should_expose_roles_status_and_lockout_information()
    {
        var admin = await CreateUserAsync("listing-admin", RoleConstants.Admin);
        var suspended = await CreateUserAsync("listing-suspended", RoleConstants.User, RoleConstants.Moderator);
        await Factory.ExecuteScopeAsync(services => DatabaseSeeder.SetUserStatusAsync(services, suspended.Id, UserStatus.Suspended, DateTime.UtcNow.AddDays(3)));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        var response = await authentication.Client.GetDataAsync<PagedResponse<Application.Dtos.User.AdminUserDto>>("/api/User/getAllUsers?page=1&size=20&search=listing-suspended");

        var item = response!.Items.Should().ContainSingle().Subject;
        item.Status.Should().Be(UserStatus.Suspended);
        item.SuspendedUntil.Should().NotBeNull();
        item.IsLockedOut.Should().BeFalse();
        item.Roles.Should().BeEquivalentTo(new[] { RoleConstants.Moderator, RoleConstants.User });
    }

    [Fact]
    public async Task Admin_user_listing_should_be_closed_to_non_admins()
    {
        var user = await CreateUserAsync("listing-regular-user");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        (await authentication.Client.GetAsync("/api/User/getAllUsers?page=1&size=20")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Assign_role_should_replace_the_previous_role_set()
    {
        var admin = await CreateUserAsync("replace-role-admin", RoleConstants.Admin);
        var target = await CreateUserAsync("replace-role-target", RoleConstants.User, RoleConstants.Moderator);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/User/assignRoleToUser", new Application.Features.Users.Commands.AssignRoleToUser.AssignRoleToUserCommand
        {
            TargetUserId = target.Id,
            Roles = new[] { RoleConstants.User }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await authentication.Client.GetDataAsync<Application.Features.Users.Queries.GetRolesToUser.GetRolesToUserQueryResponse>($"/api/User/getRolesToUser/{target.Id}");
        roles!.Roles.Should().BeEquivalentTo(new[] { RoleConstants.User });
    }

    [Fact]
    public async Task Assign_role_to_a_missing_user_should_return_not_found()
    {
        var admin = await CreateUserAsync("missing-target-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/User/assignRoleToUser", new Application.Features.Users.Commands.AssignRoleToUser.AssignRoleToUserCommand
        {
            TargetUserId = 999999,
            Roles = new[] { RoleConstants.User }
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
