using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Abstractions.Services.Configurations;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Dtos.Configurations;
using buduns_server.Domain.Enums;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Infrastructure.Seeding;

/// <summary>
/// Yetki katalogu uygulama acilisinda seeder tarafindan kodla esitleniyor.
/// Sozlesmesinin iki yarisi var ve ikisi de kritik: eksigi tamamlamak, var
/// olana dokunmamak.
/// </summary>
public sealed class EndpointSeederTests : IntegrationTestBase
{
    private const string CreatePostCode = "POST.Writing.CreatePost";
    private const string GetReportsCode = "GET.Reading.GetReports";
    private const string AssignRoleEndpointCode = "POST.Writing.AssignRoleEndpoint";

    public EndpointSeederTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Seeding_should_create_a_record_for_every_declared_endpoint()
    {
        var declared = await GetDeclaredActionsAsync();
        var stored = await GetStoredCodesAsync();

        stored.Should().BeEquivalentTo(declared.Select(action => action.Code));
    }

    [Fact]
    public async Task Seeding_should_create_every_declared_menu()
    {
        var declaredMenus = await Factory.ExecuteScopeAsync(services => Task.FromResult(
            services.GetRequiredService<IApplicationService>()
                .GetAuthorizeDefinitionEndpoints(typeof(WebAPI.Program))
                .Select(menu => menu.Name)
                .ToList()));

        var storedMenus = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Menus.AsNoTracking().Select(menu => menu.Name).ToListAsync());

        storedMenus.Should().BeEquivalentTo(declaredMenus);
    }

    [Fact]
    public async Task Seeding_should_apply_the_roles_of_the_declared_access_level()
    {
        (await GetStoredRolesAsync(CreatePostCode)).Should().BeEquivalentTo(new[] { RoleConstants.User, RoleConstants.Moderator });
        (await GetStoredRolesAsync(GetReportsCode)).Should().BeEquivalentTo(new[] { RoleConstants.Moderator });
        (await GetStoredRolesAsync(AssignRoleEndpointCode)).Should().BeEmpty();
    }

    [Fact]
    public async Task Every_stored_endpoint_should_carry_exactly_the_roles_of_its_access_level()
    {
        var declared = await GetDeclaredActionsAsync();

        foreach (var action in declared)
        {
            var stored = await GetStoredRolesAsync(action.Code);
            stored.Should().BeEquivalentTo(RoleConstants.GetDefaultRoles(action.AccessLevel), $"{action.Code} ({action.AccessLevel})");
        }
    }

    [Fact]
    public async Task Running_the_seeder_again_should_not_duplicate_records()
    {
        var declared = await GetDeclaredActionsAsync();

        await RunSeederAsync();
        await RunSeederAsync();

        (await GetStoredCodesAsync()).Should().HaveCount(declared.Count);
    }

    /// <summary>
    /// Fazin en kritik muhafazasi: yonetim ucundan yapilmis bir atama, bir
    /// sonraki acilista varsayilana geri donmemeli. Aksi halde yetki yonetimi
    /// her deploy'da sifirlanirdi.
    /// </summary>
    [Fact]
    public async Task Seeding_should_not_overwrite_a_manually_changed_role_set()
    {
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.SetEndpointRolesAsync(services, CreatePostCode, RoleConstants.Moderator));

        await RunSeederAsync();

        (await GetStoredRolesAsync(CreatePostCode)).Should().BeEquivalentTo(new[] { RoleConstants.Moderator });
    }

    [Fact]
    public async Task Seeding_should_not_reopen_an_endpoint_closed_to_every_role()
    {
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.SetEndpointRolesAsync(services, CreatePostCode));

        await RunSeederAsync();

        (await GetStoredRolesAsync(CreatePostCode)).Should().BeEmpty();
    }

    [Fact]
    public async Task Seeding_should_restore_a_deleted_record_with_its_default_roles()
    {
        await Factory.ExecuteScopeAsync(services => PermissionSeeder.DeleteEndpointAsync(services, CreatePostCode));

        var result = await RunSeederAsync();

        result.CreatedEndpointCount.Should().Be(1);
        (await GetStoredRolesAsync(CreatePostCode)).Should().BeEquivalentTo(new[] { RoleConstants.User, RoleConstants.Moderator });
    }

    /// <summary>
    /// Kodda karsiligi kalmayan kayit silinmez, raporlanir. Bir Definition
    /// metni degistiginde eski kod boyle gorunur hale gelir.
    /// </summary>
    [Fact]
    public async Task Seeding_should_report_records_that_no_longer_exist_in_code()
    {
        await Factory.ExecuteScopeAsync(async services =>
        {
            var context = services.GetRequiredService<BudunsDbContext>();
            var menu = await context.Menus.FirstAsync();
            context.Endpoints.Add(new Domain.Entities.Endpoint
            {
                Code = "POST.Writing.KaldirilmisUc",
                ActionType = nameof(ActionType.Writing),
                HttpType = "POST",
                Definition = "Kaldirilmis Uc",
                Menu = menu,
                CreatedAt = DateTime.UtcNow,
                isActive = true,
                isDeleted = false
            });
            await context.SaveChangesAsync();
        });

        var result = await RunSeederAsync();

        result.OrphanCodes.Should().ContainSingle().Which.Should().Be("POST.Writing.KaldirilmisUc");
        (await GetStoredCodesAsync()).Should().Contain("POST.Writing.KaldirilmisUc");
    }

    [Fact]
    public async Task Seeding_should_refresh_descriptive_fields_without_touching_roles()
    {
        await Factory.ExecuteScopeAsync(async services =>
        {
            var context = services.GetRequiredService<BudunsDbContext>();
            var endpoint = await context.Endpoints.SingleAsync(item => item.Code == CreatePostCode);
            endpoint.Definition = "Eski Tanim";
            endpoint.HttpType = "GET";
            await context.SaveChangesAsync();
        });

        var result = await RunSeederAsync();

        result.UpdatedEndpointCount.Should().Be(1);
        var stored = await Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Endpoints.AsNoTracking().SingleAsync(item => item.Code == CreatePostCode));
        stored.Definition.Should().Be("Create Post");
        stored.HttpType.Should().Be("POST");
        (await GetStoredRolesAsync(CreatePostCode)).Should().BeEquivalentTo(new[] { RoleConstants.User, RoleConstants.Moderator });
    }

    private Task<EndpointSeedResult> RunSeederAsync() =>
        Factory.ExecuteScopeAsync(services => services.GetRequiredService<IEndpointSeeder>().SeedAsync(typeof(WebAPI.Program), CancellationToken.None));

    private Task<List<Application.Dtos.Configurations.Action>> GetDeclaredActionsAsync() =>
        Factory.ExecuteScopeAsync(services => Task.FromResult(
            services.GetRequiredService<IApplicationService>()
                .GetAuthorizeDefinitionEndpoints(typeof(WebAPI.Program))
                .SelectMany(menu => menu.Actions)
                .ToList()));

    private Task<List<string>> GetStoredCodesAsync() =>
        Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Endpoints.AsNoTracking().Select(endpoint => endpoint.Code).ToListAsync());

    private Task<List<string>> GetStoredRolesAsync(string code) =>
        Factory.ExecuteScopeAsync(services => PermissionSeeder.GetRoleNamesForEndpointAsync(services, code));
}
