using buduns_server.Application.Common.Consts;
using buduns_server.Domain.Enums;

namespace buduns_server.UnitTests.Helpers;

/// <summary>
/// Erisim seviyesi -> rol esleme tek yerde duruyor; hem acilistaki seeder hem
/// istek anindaki fallback bu metodu okuyor. Buradaki bir kayma ikisini birden
/// kaydirir.
/// </summary>
public class RoleConstantsTests
{
    [Fact]
    public void Member_level_should_cover_every_signed_in_role()
    {
        Assert.Equal(new[] { RoleConstants.User, RoleConstants.Moderator }, RoleConstants.GetDefaultRoles(EndpointAccessLevel.Member));
    }

    [Fact]
    public void Moderator_level_should_only_cover_moderators()
    {
        Assert.Equal(new[] { RoleConstants.Moderator }, RoleConstants.GetDefaultRoles(EndpointAccessLevel.Moderator));
    }

    [Fact]
    public void Admin_only_level_should_cover_no_role()
    {
        Assert.Empty(RoleConstants.GetDefaultRoles(EndpointAccessLevel.AdminOnly));
    }

    /// <summary>
    /// Admin hicbir listede yok; yetki filtresi Admin'i kontrolden once
    /// geciriyor. Listeye eklenirse ayni yetki iki yerden gelir ve birini
    /// kaldirmak sessizce etkisiz kalir.
    /// </summary>
    [Theory]
    [InlineData(EndpointAccessLevel.AdminOnly)]
    [InlineData(EndpointAccessLevel.Moderator)]
    [InlineData(EndpointAccessLevel.Member)]
    public void No_level_should_list_the_admin_role(EndpointAccessLevel accessLevel)
    {
        Assert.DoesNotContain(RoleConstants.Admin, RoleConstants.GetDefaultRoles(accessLevel));
    }

    [Fact]
    public void Default_access_level_should_be_the_closed_one()
    {
        // Seviyesi belirtilmeyen yeni bir endpoint acik degil, kapali dogmali.
        Assert.Equal(EndpointAccessLevel.AdminOnly, default(EndpointAccessLevel));
    }

    [Fact]
    public void Every_access_level_should_resolve_to_known_system_roles()
    {
        foreach (EndpointAccessLevel accessLevel in Enum.GetValues<EndpointAccessLevel>())
        {
            Assert.All(RoleConstants.GetDefaultRoles(accessLevel), role => Assert.True(RoleConstants.IsSystemRole(role)));
        }
    }
}
