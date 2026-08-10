using buduns_server.Application.Common.Consts;
using buduns_server.Domain.Enums;
using buduns_server.Infrastructure.Services.Configurations;
using WebAPI = buduns_server.WebAPI;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Yetki kodu, endpoint'in <c>Definition</c> metninden turetiliyor. Metin
/// degisirse kod da degisir; veritabanindaki kayit oksuz kalir ve o uc icin
/// elle yapilmis butun rol atamalari sessizce gecersiz olur.
///
/// Bu dosya o sessizligi kaldirir: kod kumesi burada sabittir. Bir Definition
/// degistiginde test kirmizi doner ve degistiren kisi, bunun bir goruntu
/// duzenlemesi degil bir yetki sozlesmesi degisikligi oldugunu gorur.
/// Degisiklik bilincliyse listeyi guncellemek yeterlidir; mevcut kurulumlarda
/// eski kayit oksuz kalacagi icin acilistaki drift uyarisi da bunu raporlar.
///
/// Ikinci gorevi erisim seviyelerini kilitlemek: bir ucun Member'a acilmasi
/// ya da kapanmasi da bu listeden gecer, gozden kacan bir attribute
/// duzenlemesiyle degil.
/// </summary>
public class PermissionCodeContractTests
{
    private static readonly IReadOnlyDictionary<string, EndpointAccessLevel> KnownPermissions = new Dictionary<string, EndpointAccessLevel>(StringComparer.Ordinal)
    {
        ["DELETE.Deleting.DeleteBookmark"] = EndpointAccessLevel.Member,
        ["DELETE.Deleting.DeleteComment"] = EndpointAccessLevel.Member,
        ["DELETE.Deleting.DeleteLike"] = EndpointAccessLevel.Member,
        ["DELETE.Deleting.DeleteNotification"] = EndpointAccessLevel.Member,
        ["DELETE.Deleting.DeletePost"] = EndpointAccessLevel.Member,
        ["DELETE.Deleting.DeleteRole"] = EndpointAccessLevel.AdminOnly,
        ["DELETE.Deleting.DeleteTag"] = EndpointAccessLevel.Member,
        ["DELETE.Deleting.UnfollowUser"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetAllRoles"] = EndpointAccessLevel.AdminOnly,
        ["GET.Reading.GetAllUsers"] = EndpointAccessLevel.AdminOnly,
        ["GET.Reading.GetAuthorizeDefinitionEndpoints"] = EndpointAccessLevel.AdminOnly,
        ["GET.Reading.GetBookmarkStatus"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetBookmarks"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetFollowStatus"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetFollowingPosts"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetLikeStatus"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetLikesByPostId"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetMyLikedPosts"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetMyNotifications"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetMyPosts"] = EndpointAccessLevel.Member,
        ["GET.Reading.GetReportById"] = EndpointAccessLevel.Moderator,
        ["GET.Reading.GetReports"] = EndpointAccessLevel.Moderator,
        ["GET.Reading.GetRoleById"] = EndpointAccessLevel.AdminOnly,
        ["GET.Reading.GetRolesToUser"] = EndpointAccessLevel.AdminOnly,
        ["GET.Reading.GetUnreadNotificationCount"] = EndpointAccessLevel.Member,
        ["PATCH.Updating.MarkAllNotificationsAsRead"] = EndpointAccessLevel.Member,
        ["PATCH.Updating.MarkNotificationAsRead"] = EndpointAccessLevel.Member,
        ["POST.Reading.GetRolesToEndpoint"] = EndpointAccessLevel.AdminOnly,
        ["POST.Updating.ChangeEmail"] = EndpointAccessLevel.Member,
        ["POST.Updating.ReviewReport"] = EndpointAccessLevel.Moderator,
        ["POST.Updating.UpdateUserEmail"] = EndpointAccessLevel.Member,
        ["POST.Updating.UpdateUserMailVerify"] = EndpointAccessLevel.Member,
        ["POST.Updating.UpdateUserProfile"] = EndpointAccessLevel.Member,
        ["POST.Writing.AssignRoleEndpoint"] = EndpointAccessLevel.AdminOnly,
        ["POST.Writing.AssignRoleToUser"] = EndpointAccessLevel.AdminOnly,
        ["POST.Writing.CreateBookmark"] = EndpointAccessLevel.Member,
        ["POST.Writing.CreateComment"] = EndpointAccessLevel.Member,
        ["POST.Writing.CreateCommentReport"] = EndpointAccessLevel.Member,
        ["POST.Writing.CreateLike"] = EndpointAccessLevel.Member,
        ["POST.Writing.CreatePost"] = EndpointAccessLevel.Member,
        ["POST.Writing.CreatePostReport"] = EndpointAccessLevel.Member,
        ["POST.Writing.CreateRole"] = EndpointAccessLevel.AdminOnly,
        ["POST.Writing.CreateTag"] = EndpointAccessLevel.Member,
        ["POST.Writing.CreateUserReport"] = EndpointAccessLevel.Member,
        ["POST.Writing.FollowUser"] = EndpointAccessLevel.Member,
        ["POST.Writing.SendMailVerify"] = EndpointAccessLevel.Member,
        ["PUT.Updating.UpdateComment"] = EndpointAccessLevel.Member,
        ["PUT.Updating.UpdatePost"] = EndpointAccessLevel.Member,
        ["PUT.Updating.UpdateRole"] = EndpointAccessLevel.AdminOnly,
        ["PUT.Updating.UpdateTag"] = EndpointAccessLevel.Member
    };

    private static readonly List<Application.Dtos.Configurations.Action> Actions =
        new ApplicationService().GetAuthorizeDefinitionEndpoints(typeof(WebAPI.Program))
            .SelectMany(menu => menu.Actions)
            .ToList();

    [Fact]
    public void Permission_codes_should_match_the_recorded_contract()
    {
        var actual = Actions.Select(action => action.Code).OrderBy(code => code, StringComparer.Ordinal).ToArray();
        var expected = KnownPermissions.Keys.OrderBy(code => code, StringComparer.Ordinal).ToArray();

        var added = actual.Except(expected, StringComparer.Ordinal).ToArray();
        var removed = expected.Except(actual, StringComparer.Ordinal).ToArray();

        Assert.True(
            added.Length == 0 && removed.Length == 0,
            $"Yetki kodu kumesi degisti. Eklenen: [{string.Join(", ", added)}], kaybolan: [{string.Join(", ", removed)}]. " +
            "Bu bilincli bir degisiklikse listeyi guncelleyin; degilse bir Definition metni degistirilmis olabilir.");
    }

    [Fact]
    public void Access_levels_should_match_the_recorded_contract()
    {
        var drifted = Actions
            .Where(action => KnownPermissions.TryGetValue(action.Code, out var level) && level != action.AccessLevel)
            .Select(action => $"{action.Code}: {KnownPermissions[action.Code]} -> {action.AccessLevel}")
            .ToArray();

        Assert.True(drifted.Length == 0, $"Erisim seviyesi degisen uclar: {string.Join(", ", drifted)}");
    }

    [Fact]
    public void Every_permission_should_resolve_to_the_roles_of_its_access_level()
    {
        // Seeder, kaydi yeni olusturulan uce bu listeyi yaziyor; filtre de
        // kayit bulunamadiginda ayni listeye dusuyor. Iki taraf ayni kaynagi
        // okudugu icin burada tek bir dogrulama yeterli.
        foreach (var action in Actions)
        {
            Assert.Equal(RoleConstants.GetDefaultRoles(action.AccessLevel), action.DefaultRoles);
        }
    }

    [Fact]
    public void Admin_only_permissions_should_not_open_to_any_role()
    {
        foreach (var action in Actions.Where(action => action.AccessLevel == EndpointAccessLevel.AdminOnly))
        {
            Assert.Empty(action.DefaultRoles);
        }
    }
}
