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
///
/// Faz 8'den beri kayitlar acilistaki EndpointSeeder tarafindan olusuyor;
/// testler artik yetki dagitmiyor, var olani kisitlayip sonucu olcuyor.
/// </summary>
public sealed class EndpointPermissionTests : IntegrationTestBase
{
    private const string CreatePostCode = "POST.Writing.CreatePost";
    private const string GetMyPostsCode = "GET.Reading.GetMyPosts";

    public EndpointPermissionTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    /// <summary>
    /// Fazin asil sonucu: bos bir veritabaniyla acilan sistemde sirali bir
    /// kullanici, hicbir elle atama yapilmadan uye ucunu kullanabiliyor.
    /// </summary>
    [Fact]
    public async Task Member_endpoints_should_work_without_any_manual_assignment()
    {
        var user = await CreateUserAsync("seeded-permission-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Endpoint_permission_should_gate_a_single_action_for_a_regular_user()
    {
        var user = await CreateUserAsync("permission-user");
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        // Rolleri bosaltmak ucu kapatir; geri eklemek acar.
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.SetEndpointRolesAsync(services, CreatePostCode));
        var whileClosed = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        await Factory.ExecuteScopeAsync(services => PermissionSeeder.SetEndpointRolesAsync(services, CreatePostCode, RoleConstants.User));
        var whileOpen = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        whileClosed.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        whileOpen.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Permission_granted_to_one_role_should_not_leak_to_another()
    {
        var user = await CreateUserAsync("leak-check-user");
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.SetEndpointRolesAsync(services, CreatePostCode, RoleConstants.Moderator));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_should_bypass_endpoint_permissions_entirely()
    {
        var admin = await CreateUserAsync("bypass-admin", RoleConstants.Admin);
        using var authentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);

        // Uc herkese kapatildi; Admin yine de geciyor.
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.SetEndpointRolesAsync(services, CreatePostCode));
        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Closing_one_action_should_not_affect_the_others()
    {
        var user = await CreateUserAsync("scoped-permission-user");
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.SetEndpointRolesAsync(services, GetMyPostsCode));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var allowed = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });
        var forbidden = await authentication.Client.GetAsync("/api/Post/me?page=1&size=20");

        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Kayit silinirse yetki kaybolmaz; karar kodda bildirilen seviyeye duser.
    /// "Kapat" demenin dogru yolu kaydi silmek degil, rolleri bosaltmaktir.
    /// </summary>
    [Fact]
    public async Task Deleting_the_record_should_fall_back_to_the_access_level_declared_in_code()
    {
        var user = await CreateUserAsync("fallback-user");
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.DeleteEndpointAsync(services, CreatePostCode));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Varsayilani AdminOnly olan bir ucun kaydi silinirse fallback hicbir
    /// role acilmaz; fail-open olmadigi burada dogrulaniyor.
    /// </summary>
    [Fact]
    public async Task Deleting_an_admin_only_record_should_not_open_it_to_anyone()
    {
        var moderator = await CreateUserAsync("fallback-moderator", RoleConstants.Moderator);
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.DeleteEndpointAsync(services, "POST.Writing.AssignRoleEndpoint"));
        using var authentication = await Factory.CreateAuthenticatedClientAsync(moderator.Id);

        var response = await authentication.Client.PostAsJsonAsync("/api/AuthorizationEndpoints", new AssignRoleEndpointCommand
        {
            Roles = new[] { RoleConstants.Moderator },
            Menu = AuthorizeDefinitionConstants.Posts,
            Code = CreatePostCode
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

        var menus = await authentication.Client.GetDataAsync<List<Menu>>("/api/ApplicationService");

        menus.Should().NotBeNullOrEmpty();
        menus!.Single(menu => menu.Name == AuthorizeDefinitionConstants.Posts)
            .Actions.Should().Contain(action => action.Code == CreatePostCode);
    }

    [Fact]
    public async Task Application_service_endpoint_should_be_closed_to_non_admins()
    {
        var user = await CreateUserAsync("definition-user");
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
        var payload = await rolesResponse.ReadDataAsync<JsonElement>();
        payload.GetProperty("roles").EnumerateArray().Select(item => item.GetString()).Should().BeEquivalentTo(new[] { RoleConstants.Moderator });

        var storedRoles = await Factory.ExecuteScopeAsync(services => PermissionSeeder.GetRoleNamesForEndpointAsync(services, CreatePostCode));
        storedRoles.Should().BeEquivalentTo(new[] { RoleConstants.Moderator });
    }

    /// <summary>
    /// Atama, mevcut rol kumesinin uzerine eklemez; onu degistirir. Faz 8'e
    /// kadar bu yol `InvalidOperationException` atiyordu (rol koleksiyonu
    /// yineleme sirasinda degistiriliyordu).
    /// </summary>
    [Fact]
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

    /// <summary>
    /// Endpoint -> rol eslemesi onbelleklendigi icin atamanin onbellegi de
    /// dusurmesi gerekiyor; dusurmezse yeni yetki TTL dolana kadar islemez.
    /// </summary>
    [Fact]
    public async Task Assignment_should_take_effect_on_the_very_next_request()
    {
        var admin = await CreateUserAsync("cache-invalidation-admin", RoleConstants.Admin);
        var user = await CreateUserAsync("cache-invalidation-user");
        using var adminAuthentication = await Factory.CreateAuthenticatedClientAsync(admin.Id);
        using var userAuthentication = await Factory.CreateAuthenticatedClientAsync(user.Id);

        // Ilk istek sonucu onbellege alir.
        (await userAuthentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        (await adminAuthentication.Client.PostAsJsonAsync("/api/AuthorizationEndpoints", new AssignRoleEndpointCommand
        {
            Roles = new[] { RoleConstants.Moderator },
            Menu = AuthorizeDefinitionConstants.Posts,
            Code = CreatePostCode
        })).EnsureSuccessStatusCode();

        var afterAssignment = await userAuthentication.Client.PostAsJsonAsync("/api/Post/create", new CreatePostsCommand { Content = "icerik" });

        afterAssignment.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
        // Katalog acilista dolduruldugu icin kaydi olmayan bir uc bulmak artik
        // kaydi silmeyi gerektiriyor.
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.DeleteEndpointAsync(services, CreatePostCode));
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
