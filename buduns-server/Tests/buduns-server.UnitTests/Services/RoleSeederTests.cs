using buduns_server.Application.Common.Consts;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Persistence.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Seeder'in tek sozu var: eksik sistem rolunu ekler, var olana dokunmaz.
/// Ikinci kisim onemli, cunku her uygulama acilisinda calisiyor.
/// </summary>
public class RoleSeederTests
{
    [Fact]
    public async Task SeedAsync_RolesMissing_ShouldCreateEverySystemRole()
    {
        var roleManager = CreateRoleManager();
        roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(false);
        var createdNames = new List<string>();
        roleManager.CreateAsync(Arg.Do<Role>(role => createdNames.Add(role.Name!))).Returns(IdentityResult.Success);

        await new RoleSeeder(roleManager, NullLogger<RoleSeeder>.Instance).SeedAsync(CancellationToken.None);

        Assert.Equal(RoleConstants.SystemRoles, createdNames);
    }

    [Fact]
    public async Task SeedAsync_RolesAlreadyExist_ShouldNotWriteAnything()
    {
        var roleManager = CreateRoleManager();
        roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(true);

        await new RoleSeeder(roleManager, NullLogger<RoleSeeder>.Instance).SeedAsync(CancellationToken.None);

        await roleManager.DidNotReceiveWithAnyArgs().CreateAsync(default!);
    }

    [Fact]
    public async Task SeedAsync_SingleRoleMissing_ShouldCreateOnlyThatRole()
    {
        var roleManager = CreateRoleManager();
        roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(true);
        roleManager.RoleExistsAsync(RoleConstants.Moderator).Returns(false);
        roleManager.CreateAsync(Arg.Any<Role>()).Returns(IdentityResult.Success);

        await new RoleSeeder(roleManager, NullLogger<RoleSeeder>.Instance).SeedAsync(CancellationToken.None);

        await roleManager.Received(1).CreateAsync(Arg.Is<Role>(role => role.Name == RoleConstants.Moderator));
    }

    [Fact]
    public async Task SeedAsync_CreateFails_ShouldThrowSoStartupStops()
    {
        var roleManager = CreateRoleManager();
        roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(false);
        roleManager.CreateAsync(Arg.Any<Role>()).Returns(IdentityResult.Failed(new IdentityError { Description = "Rol adi gecersiz." }));
        var seeder = new RoleSeeder(roleManager, NullLogger<RoleSeeder>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync(CancellationToken.None));

        Assert.Contains("Rol adi gecersiz.", exception.Message);
    }

    /// <summary>
    /// Baska bir instance ayni rolu araya girip olusturmus olabilir; bu durumda
    /// olusturma hatasi yutulur ve acilis devam eder.
    /// </summary>
    [Fact]
    public async Task SeedAsync_RoleAppearsBetweenCheckAndCreate_ShouldContinue()
    {
        var roleManager = CreateRoleManager();
        roleManager.RoleExistsAsync(Arg.Any<string>()).Returns(_ => false, _ => true);
        roleManager.CreateAsync(Arg.Any<Role>()).Returns(IdentityResult.Failed(new IdentityError { Description = "Role already exists." }));

        await new RoleSeeder(roleManager, NullLogger<RoleSeeder>.Instance).SeedAsync(CancellationToken.None);
    }

    private static RoleManager<Role> CreateRoleManager() =>
        Substitute.For<RoleManager<Role>>(Substitute.For<IRoleStore<Role>>(), null!, null!, null!, null!);
}
