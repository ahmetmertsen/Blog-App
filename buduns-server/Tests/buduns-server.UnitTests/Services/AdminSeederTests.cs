using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.Options;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Persistence.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Bootstrap yukseltmesi yetki dagitiyor; bu yuzden asil deger tasiyan testler
/// yukseltmenin NE ZAMAN YAPILMADIGINI dogrulayanlar.
/// </summary>
public class AdminSeederTests
{
    private const string ConfiguredEmail = "bootstrap@buduns.test";

    [Fact]
    public async Task SeedAsync_EmailNotConfigured_ShouldDoNothing()
    {
        var userManager = CreateUserManager();

        await CreateSeeder(userManager, email: null).SeedAsync(CancellationToken.None);

        await userManager.DidNotReceiveWithAnyArgs().GetUsersInRoleAsync(default!);
        await userManager.DidNotReceiveWithAnyArgs().AddToRoleAsync(default!, default!);
    }

    [Fact]
    public async Task SeedAsync_SystemAlreadyHasAnAdmin_ShouldNotPromoteAnyone()
    {
        var userManager = CreateUserManager();
        userManager.GetUsersInRoleAsync(RoleConstants.Admin).Returns(new List<User> { new() { Id = 7, FullName = "Mevcut Admin" } });

        await CreateSeeder(userManager, ConfiguredEmail).SeedAsync(CancellationToken.None);

        await userManager.DidNotReceiveWithAnyArgs().AddToRoleAsync(default!, default!);
    }

    [Fact]
    public async Task SeedAsync_ConfiguredAccountIsNotRegistered_ShouldSkipWithoutThrowing()
    {
        var userManager = CreateUserManager();
        userManager.GetUsersInRoleAsync(RoleConstants.Admin).Returns(new List<User>());
        userManager.FindByEmailAsync(ConfiguredEmail).Returns((User?)null);

        await CreateSeeder(userManager, ConfiguredEmail).SeedAsync(CancellationToken.None);

        await userManager.DidNotReceiveWithAnyArgs().AddToRoleAsync(default!, default!);
    }

    [Fact]
    public async Task SeedAsync_NoAdminAndAccountExists_ShouldPromoteIt()
    {
        var candidate = new User { Id = 42, Email = ConfiguredEmail, FullName = "Bootstrap Aday" };
        var userManager = CreateUserManager();
        userManager.GetUsersInRoleAsync(RoleConstants.Admin).Returns(new List<User>());
        userManager.FindByEmailAsync(ConfiguredEmail).Returns(candidate);
        userManager.AddToRoleAsync(candidate, RoleConstants.Admin).Returns(IdentityResult.Success);

        await CreateSeeder(userManager, ConfiguredEmail).SeedAsync(CancellationToken.None);

        await userManager.Received(1).AddToRoleAsync(candidate, RoleConstants.Admin);
    }

    [Fact]
    public async Task SeedAsync_PromotionFails_ShouldThrow()
    {
        var candidate = new User { Id = 42, Email = ConfiguredEmail, FullName = "Bootstrap Aday" };
        var userManager = CreateUserManager();
        userManager.GetUsersInRoleAsync(RoleConstants.Admin).Returns(new List<User>());
        userManager.FindByEmailAsync(ConfiguredEmail).Returns(candidate);
        userManager.AddToRoleAsync(candidate, RoleConstants.Admin).Returns(IdentityResult.Failed(new IdentityError { Description = "Rol bulunamadi." }));
        var seeder = CreateSeeder(userManager, ConfiguredEmail);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync(CancellationToken.None));

        Assert.Contains("Rol bulunamadi.", exception.Message);
    }

    private static AdminSeeder CreateSeeder(UserManager<User> userManager, string? email) =>
        new(userManager, Options.Create(new BootstrapAdminOptions { Email = email }), NullLogger<AdminSeeder>.Instance);

    private static UserManager<User> CreateUserManager() =>
        Substitute.For<UserManager<User>>(Substitute.For<IUserStore<User>>(), null!, null!, null!, null!, null!, null!, null!, null!);
}
