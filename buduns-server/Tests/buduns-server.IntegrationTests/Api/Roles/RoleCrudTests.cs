using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos.Role;
using buduns_server.Application.Features.Roles.Commands.Create;
using buduns_server.Application.Features.Roles.Commands.Update;
using buduns_server.Application.Features.Users.Commands.AssignRoleToUser;
using buduns_server.Application.Features.Users.Queries.GetRolesToUser;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Api.Roles;

/// <summary>
/// Rol yonetimi tamamen Admin'e kapali bir alan. Sistem rollerinin (Admin,
/// Moderator, User) degistirilememesi ve kullanilan rollerin silinememesi
/// yetki modelinin butunlugunu koruyan kurallar.
/// </summary>
public sealed class RoleCrudTests : IntegrationTestBase
{
    public RoleCrudTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Admin_should_create_list_update_and_delete_a_custom_role()
    {
        var admin = await CreateUserAsync("role-admin-user", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        (await authentication.Client.PostAsJsonAsync("/api/Role/create", new CreateRoleCommand { Name = "  Editor  " })).StatusCode.Should().Be(HttpStatusCode.OK);

        var roles = await authentication.Client.GetDataAsync<List<RoleDto>>("/api/Role/getAll");
        var editor = roles!.Single(role => role.Name == "Editor");

        var byId = await authentication.Client.GetDataAsync<RoleDto>($"/api/Role/getById/{editor.Id}");
        byId!.Name.Should().Be("Editor");

        (await authentication.Client.PutAsJsonAsync("/api/Role/update", new UpdateRoleCommand { Id = editor.Id, Name = "Reviewer" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await authentication.Client.DeleteAsync($"/api/Role/delete/{editor.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        var remaining = await authentication.Client.GetDataAsync<List<RoleDto>>("/api/Role/getAll");
        remaining!.Should().OnlyContain(role => role.Name != "Reviewer");
    }

    [Fact]
    public async Task Duplicate_role_name_should_be_rejected_case_insensitively()
    {
        var admin = await CreateUserAsync("duplicate-role-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);
        (await authentication.Client.PostAsJsonAsync("/api/Role/create", new CreateRoleCommand { Name = "Editor" })).EnsureSuccessStatusCode();

        var response = await authentication.Client.PostAsJsonAsync("/api/Role/create", new CreateRoleCommand { Name = "editor" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(RoleConstants.Admin)]
    [InlineData(RoleConstants.Moderator)]
    [InlineData(RoleConstants.User)]
    public async Task System_roles_should_not_be_updated_or_deleted(string roleName)
    {
        var admin = await CreateUserAsync("system-role-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);
        var roleId = await Factory.ExecuteScopeAsync(async services =>
            (await services.GetRequiredService<BudunsDbContext>().Roles.AsNoTracking().SingleAsync(role => role.Name == roleName)).Id);

        var updated = await authentication.Client.PutAsJsonAsync("/api/Role/update", new UpdateRoleCommand { Id = roleId, Name = "YeniAd" });
        var deleted = await authentication.Client.DeleteAsync($"/api/Role/delete/{roleId}");

        updated.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        deleted.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Role_assigned_to_a_user_should_not_be_deleted()
    {
        var admin = await CreateUserAsync("assigned-role-admin", RoleConstants.Admin);
        var target = await CreateUserAsync("assigned-role-target");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);
        (await authentication.Client.PostAsJsonAsync("/api/Role/create", new CreateRoleCommand { Name = "Editor" })).EnsureSuccessStatusCode();
        var roles = await authentication.Client.GetDataAsync<List<RoleDto>>("/api/Role/getAll");
        var editor = roles!.Single(role => role.Name == "Editor");

        (await authentication.Client.PostAsJsonAsync("/api/User/assignRoleToUser", new AssignRoleToUserCommand { TargetUserId = target.Id, Roles = new[] { "Editor" } })).EnsureSuccessStatusCode();

        var response = await authentication.Client.DeleteAsync($"/api/Role/delete/{editor.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Role_bound_to_an_endpoint_permission_should_not_be_deleted()
    {
        var admin = await CreateUserAsync("endpoint-role-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);
        (await authentication.Client.PostAsJsonAsync("/api/Role/create", new CreateRoleCommand { Name = "Editor" })).EnsureSuccessStatusCode();
        var roles = await authentication.Client.GetDataAsync<List<RoleDto>>("/api/Role/getAll");
        var editor = roles!.Single(role => role.Name == "Editor");
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.GrantEndpointAsync(services, AuthorizeDefinitionConstants.Posts, "POST.Writing.CreatePost", "Editor"));

        var response = await authentication.Client.DeleteAsync($"/api/Role/delete/{editor.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_role_should_return_not_found()
    {
        var admin = await CreateUserAsync("missing-role-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        (await authentication.Client.GetAsync("/api/Role/getById/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await authentication.Client.PutAsJsonAsync("/api/Role/update", new UpdateRoleCommand { Id = 999999, Name = "Editor" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await authentication.Client.DeleteAsync("/api/Role/delete/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Role_endpoints_should_be_closed_to_non_admin_users()
    {
        var moderator = await CreateUserAsync("role-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        (await authentication.Client.GetAsync("/api/Role/getAll")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await authentication.Client.PostAsJsonAsync("/api/Role/create", new CreateRoleCommand { Name = "Editor" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Role_endpoints_should_reject_anonymous_requests()
    {
        using var client = Factory.CreateHttpsClient();

        (await client.GetAsync("/api/Role/getAll")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/Role/create", new CreateRoleCommand { Name = "Editor" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_roles_to_user_should_return_the_current_role_set()
    {
        var admin = await CreateUserAsync("roles-query-admin", RoleConstants.Admin);
        var target = await CreateUserAsync("roles-query-target");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        var response = await authentication.Client.GetDataAsync<GetRolesToUserQueryResponse>($"/api/User/getRolesToUser/{target.Id}");

        response!.UserId.Should().Be(target.Id);
        response.Roles.Should().BeEquivalentTo(new[] { RoleConstants.User });
    }

    [Fact]
    public async Task Get_roles_to_user_for_a_missing_user_should_return_not_found()
    {
        var admin = await CreateUserAsync("roles-missing-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        var response = await authentication.Client.GetAsync("/api/User/getRolesToUser/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
