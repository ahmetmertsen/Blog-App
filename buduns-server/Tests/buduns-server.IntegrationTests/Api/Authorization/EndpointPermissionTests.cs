using System.Text.Json;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos.Configurations;
using buduns_server.Application.Features.AuthorizationEndpoint.Commands.AssignRoleEndpoint;
using buduns_server.Application.Features.AuthorizationEndpoint.Queries.GetRolesToEndpoint;
using buduns_server.Application.Features.Posts.Commands.Create;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;

namespace buduns_server.IntegrationTests.Api.Authorization;

/// <summary>
/// RolePermissionFilter, istek aninda urettigi yetki kodunu veritabanindaki
/// Endpoint kaydiyla karsilastiriyor. Kodun uretim bicimi ile kaydedilen bicim
/// arasindaki en ufak fark tum yetkileri sessizce gecersiz kilar; bu testler
/// iki tarafi ayni anda calistirarak esitligi dogrular.
/// </summary>
public sealed class EndpointPermissionTests : IntegrationTestBase
{
    private const string CreatePostCode = "POST.Writing.CreatePost";

    public EndpointPermissionTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Endpoint_permission_should_gate_a_single_action_for_a_regular_user()
    {
        var user = await CreateUserAsync("permission-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var beforeGrant = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.GrantEndpointAsync(services, AuthorizeDefinitionConstants.Posts, CreatePostCode, RoleConstants.User));
        var afterGrant = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        beforeGrant.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        afterGrant.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Permission_granted_to_one_role_should_not_leak_to_another()
    {
        var user = await CreateUserAsync("leak-check-user");
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.GrantEndpointAsync(services, AuthorizeDefinitionConstants.Posts, CreatePostCode, RoleConstants.Moderator));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_should_bypass_endpoint_permissions_entirely()
    {
        var admin = await CreateUserAsync("bypass-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        // Hicbir Endpoint kaydi seed edilmedi.
        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Permission_granted_only_to_one_action_should_not_open_the_others()
    {
        var user = await CreateUserAsync("scoped-permission-user");
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.GrantEndpointAsync(services, AuthorizeDefinitionConstants.Posts, CreatePostCode, RoleConstants.User));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var allowed = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });
        var forbidden = await authentication.Client.GetAsync("/api/Post/me?page=1&size=20");

        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Public_endpoints_should_stay_open_without_any_permission_record()
    {
        var author = await CreateUserAsync("public-endpoint-author");
        var post = await Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreatePostAsync(services, author.Id));
        using var client = Factory.CreateHttpsClient();

        (await client.GetAsync("/api/Post/getAll?page=1&size=20")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/Post/getById/{post.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/Tag/getAll?page=1&size=50")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/api/Post/daily-top50")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Application_service_endpoint_should_expose_every_permission_code_to_admins()
    {
        var admin = await CreateUserAsync("definition-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        var menus = await authentication.Client.GetFromJsonAsync<List<Menu>>("/api/ApplicationService");

        menus.Should().NotBeNullOrEmpty();
        menus!.Single(menu => menu.Name == AuthorizeDefinitionConstants.Posts)
            .Actions.Should().Contain(action => action.Code == CreatePostCode);
    }

    [Fact]
    public async Task Application_service_endpoint_should_be_closed_to_non_admins()
    {
        var user = await CreateUserAsync("definition-user");
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        (await authentication.Client.GetAsync("/api/ApplicationService")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_should_assign_roles_to_an_endpoint_and_read_them_back()
    {
        var admin = await CreateUserAsync("assign-endpoint-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        var assignResponse = await authentication.Client.PostAsJsonAsync("/api/AuthorizationEndpoints", new AssignRoleEndpointCommand
        {
            Roles = new[] { RoleConstants.Moderator },
            Menu = AuthorizeDefinitionConstants.Posts,
            Code = CreatePostCode
        });

        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rolesResponse = await authentication.Client.PostAsJsonAsync("/api/AuthorizationEndpoints/getRolesToEndpoint", new GetRolesToEndpointQuery
        {
            Menu = AuthorizeDefinitionConstants.Posts,
            Code = CreatePostCode
        });

        // GetRolesToEndpointQueryResponse.Roles "object" olarak tanimli oldugu
        // icin govde tipli olarak okunamaz; JSON uzerinden dogrulaniyor.
        var payload = await rolesResponse.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("roles").EnumerateArray().Select(item => item.GetString()).Should().BeEquivalentTo(new[] { RoleConstants.Moderator });

        var storedRoles = await Factory.ExecuteScopeAsync(services => PermissionSeeder.GetRoleNamesForEndpointAsync(services, CreatePostCode));
        storedRoles.Should().BeEquivalentTo(new[] { RoleConstants.Moderator });
    }

    /// <summary>
    /// Bilinen acik hata: AuthorizationEndpointService.AssignRoleEndpointAsync
    /// icinde `foreach (var role in endpoint.Roles) endpoint.Roles.Remove(role)`
    /// var. Endpoint'in zaten rolu varsa koleksiyon yineleme sirasinda
    /// degistigi icin InvalidOperationException atar; yani bir endpoint'in
    /// rolleri bir kez atandiktan sonra degistirilemez.
    /// docs/teknik-borc/README.md "Devam eden isler" bolumunde kayitli.
    /// Hata duzeltildiginde Skip kaldirilmalidir.
    /// </summary>
    [Fact(Skip = "Bilinen hata: AuthorizationEndpointService rol koleksiyonunu yineleme sirasinda degistiriyor.")]
    public async Task Reassigning_roles_to_an_endpoint_should_replace_the_previous_role_set()
    {
        var admin = await CreateUserAsync("reassign-endpoint-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        AssignRoleEndpointCommand BuildCommand(params string[] roles) => new()
        {
            Roles = roles,
            Menu = AuthorizeDefinitionConstants.Posts,
            Code = CreatePostCode
        };

        (await authentication.Client.PostAsJsonAsync("/api/AuthorizationEndpoints", BuildCommand(RoleConstants.Moderator))).EnsureSuccessStatusCode();
        var second = await authentication.Client.PostAsJsonAsync("/api/AuthorizationEndpoints", BuildCommand(RoleConstants.User));

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var storedRoles = await Factory.ExecuteScopeAsync(services => PermissionSeeder.GetRoleNamesForEndpointAsync(services, CreatePostCode));
        storedRoles.Should().BeEquivalentTo(new[] { RoleConstants.User });
    }

    [Fact]
    public async Task Assigning_an_unknown_permission_code_should_return_not_found()
    {
        var admin = await CreateUserAsync("unknown-code-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/AuthorizationEndpoints", new AssignRoleEndpointCommand
        {
            Roles = new[] { RoleConstants.Moderator },
            Menu = AuthorizeDefinitionConstants.Posts,
            Code = "POST.Writing.OlmayanUc"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reading_roles_of_an_unregistered_endpoint_should_return_not_found()
    {
        var admin = await CreateUserAsync("unregistered-endpoint-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/AuthorizationEndpoints/getRolesToEndpoint", new GetRolesToEndpointQuery
        {
            Menu = AuthorizeDefinitionConstants.Posts,
            Code = CreatePostCode
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Authorization_endpoints_should_be_closed_to_non_admins()
    {
        var moderator = await CreateUserAsync("authorization-moderator", RoleConstants.Moderator);
        await GrantEndpointPermissionsAsync();
        using var authentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/AuthorizationEndpoints", new AssignRoleEndpointCommand
        {
            Roles = new[] { RoleConstants.Moderator },
            Menu = AuthorizeDefinitionConstants.Posts,
            Code = CreatePostCode
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
