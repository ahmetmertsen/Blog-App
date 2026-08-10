using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Domain.Entities.Identity;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Infrastructure.Seeding;

/// <summary>
/// Sistem rolleri uygulama acilisinda seeder tarafindan olusturuluyor. Testlerin
/// tamami da ayni seeder'a dayaniyor; burasi onun sozlesmesini dogruluyor.
/// </summary>
public sealed class RoleSeederTests : IntegrationTestBase
{
    public RoleSeederTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Seeding_should_create_every_system_role()
    {
        var roleNames = await GetRoleNamesAsync();

        roleNames.Should().BeEquivalentTo(RoleConstants.SystemRoles);
    }

    [Fact]
    public async Task Running_the_seeder_again_should_not_duplicate_roles()
    {
        await RunSeederAsync();
        await RunSeederAsync();

        (await GetRoleNamesAsync()).Should().BeEquivalentTo(RoleConstants.SystemRoles);
    }

    [Fact]
    public async Task Seeding_should_leave_custom_roles_untouched()
    {
        await Factory.ExecuteScopeAsync(async services =>
        {
            var roleManager = services.GetRequiredService<RoleManager<Role>>();
            (await roleManager.CreateAsync(new Role { Name = "Editor" })).Succeeded.Should().BeTrue();
        });

        await RunSeederAsync();

        (await GetRoleNamesAsync()).Should().BeEquivalentTo(RoleConstants.SystemRoles.Append("Editor"));
    }

    [Fact]
    public async Task Seeding_should_restore_a_deleted_system_role()
    {
        await Factory.ExecuteScopeAsync(async services =>
        {
            var context = services.GetRequiredService<BudunsDbContext>();
            var moderator = await context.Roles.SingleAsync(role => role.Name == RoleConstants.Moderator);
            context.Roles.Remove(moderator);
            await context.SaveChangesAsync();
        });

        await RunSeederAsync();

        (await GetRoleNamesAsync()).Should().BeEquivalentTo(RoleConstants.SystemRoles);
    }

    private Task RunSeederAsync() =>
        Factory.ExecuteScopeAsync(services => services.GetRequiredService<IRoleSeeder>().SeedAsync(CancellationToken.None));

    private Task<List<string>> GetRoleNamesAsync() =>
        Factory.ExecuteScopeAsync(async services =>
            await services.GetRequiredService<BudunsDbContext>().Roles.AsNoTracking().Select(role => role.Name!).ToListAsync());
}
