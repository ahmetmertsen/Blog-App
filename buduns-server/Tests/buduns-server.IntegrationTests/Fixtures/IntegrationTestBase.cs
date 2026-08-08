using buduns_server.Application.Common.Consts;
using buduns_server.Domain.Entities.Identity;
using buduns_server.IntegrationTests.Collections;
using buduns_server.IntegrationTests.Helpers;

namespace buduns_server.IntegrationTests.Fixtures;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected IntegrationTestBase(BudunsWebApplicationFactory factory)
    {
        Factory = factory;
    }

    protected BudunsWebApplicationFactory Factory { get; }

    public Task InitializeAsync() => Factory.ResetStateAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Admin olmayan rollerin [AuthorizeDefinition] tasiyan uclara erisebilmesi
    /// icin endpoint yetkilerini acar. Respawn her testte tablolari bosalttigi
    /// icin ihtiyac duyan her test bunu kendisi cagirmalidir.
    /// </summary>
    protected Task GrantEndpointPermissionsAsync(params string[] roleNames) =>
        Factory.ExecuteScopeAsync(services => PermissionSeeder.GrantAllEndpointsAsync(services, roleNames.Length == 0 ? new[] { RoleConstants.User, RoleConstants.Moderator } : roleNames));

    protected Task<User> CreateUserAsync(string userName, params string[] roles) =>
        Factory.ExecuteScopeAsync(services => DatabaseSeeder.CreateUserAsync(services, userName, userName.Replace('-', ' '), roles.Length == 0 ? new[] { RoleConstants.User } : roles));
}
