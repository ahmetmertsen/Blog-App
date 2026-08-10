using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Domain.Entities.Identity;
using buduns_server.IntegrationTests.Fixtures;
using buduns_server.IntegrationTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Infrastructure.Seeding;

/// <summary>
/// Ilk admin, yapilandirmadaki e-postaya sahip KAYITLI bir hesabin
/// yukseltilmesiyle doguyor. Seeder sifre uretmedigi icin bu hesabin once
/// normal kayit akisindan gecmis olmasi gerekiyor.
/// </summary>
public sealed class AdminSeederTests : IntegrationTestBase
{
    public AdminSeederTests(BudunsWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Configured_account_should_be_promoted_when_no_admin_exists()
    {
        await CreateBootstrapCandidateAsync();

        await RunSeederAsync();

        (await IsAdminAsync(BudunsWebApplicationFactory.BootstrapAdminUserName)).Should().BeTrue();
    }

    [Fact]
    public async Task Promotion_should_be_skipped_when_the_system_already_has_an_admin()
    {
        await CreateUserAsync("existing-admin", RoleConstants.Admin);
        await CreateBootstrapCandidateAsync();

        await RunSeederAsync();

        (await IsAdminAsync(BudunsWebApplicationFactory.BootstrapAdminUserName)).Should().BeFalse();
    }

    [Fact]
    public async Task Missing_account_should_not_create_a_user_or_fail()
    {
        await RunSeederAsync();

        var admins = await Factory.ExecuteScopeAsync(services =>
            services.GetRequiredService<UserManager<User>>().GetUsersInRoleAsync(RoleConstants.Admin));
        admins.Should().BeEmpty();
    }

    [Fact]
    public async Task Running_the_seeder_again_should_not_change_anything()
    {
        await CreateBootstrapCandidateAsync();
        await RunSeederAsync();

        await RunSeederAsync();

        var admins = await Factory.ExecuteScopeAsync(services =>
            services.GetRequiredService<UserManager<User>>().GetUsersInRoleAsync(RoleConstants.Admin));
        admins.Should().ContainSingle(admin => admin.UserName == BudunsWebApplicationFactory.BootstrapAdminUserName);
    }

    private Task<User> CreateBootstrapCandidateAsync() =>
        Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, BudunsWebApplicationFactory.BootstrapAdminUserName, "Bootstrap Admin", RoleConstants.User));

    private Task RunSeederAsync() =>
        Factory.ExecuteScopeAsync(services => services.GetRequiredService<IAdminSeeder>().SeedAsync(CancellationToken.None));

    private Task<bool> IsAdminAsync(string userName) =>
        Factory.ExecuteScopeAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByNameAsync(userName);
            return user != null && await userManager.IsInRoleAsync(user, RoleConstants.Admin);
        });
}
