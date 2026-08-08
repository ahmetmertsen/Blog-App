using buduns_server.Application.Common.Consts;
using buduns_server.Infrastructure.Services.Configurations;
using buduns_server.WebAPI.Controllers;
using WebAPI = buduns_server.WebAPI;

namespace buduns_server.UnitTests.Services;

/// <summary>
/// Endpoint yetki kodlari bu servisin urettigi metinlerle RolePermissionFilter'in
/// istek aninda urettigi metinlerin birebir esitligine dayaniyor. Kod formati
/// degisirse tum yetki atamalari sessizce gecersiz olur.
/// </summary>
public class ApplicationServiceTests
{
    private static readonly List<Application.Dtos.Configurations.Menu> Menus =
        new ApplicationService().GetAuthorizeDefinitionEndpoints(typeof(WebAPI.Program));

    [Fact]
    public void GetAuthorizeDefinitionEndpoints_ShouldDiscoverAllAnnotatedMenus()
    {
        var menuNames = Menus.Select(menu => menu.Name).ToArray();

        Assert.Contains(AuthorizeDefinitionConstants.Posts, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Comments, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Likes, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Bookmarks, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Followers, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Notification, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Reports, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Tags, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Roles, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Users, menuNames);
        Assert.Contains(AuthorizeDefinitionConstants.Auth, menuNames);
    }

    [Fact]
    public void GetAuthorizeDefinitionEndpoints_ShouldNotProduceDuplicateMenus()
    {
        // Ayni menu birden fazla controller'da kullanildiginda tek girdi olmali.
        Assert.Equal(Menus.Count, Menus.Select(menu => menu.Name).Distinct().Count());
    }

    [Fact]
    public void GetAuthorizeDefinitionEndpoints_CodeShouldFollowHttpMethodActionTypeDefinitionFormat()
    {
        var createPost = Menus
            .Single(menu => menu.Name == AuthorizeDefinitionConstants.Posts)
            .Actions
            .Single(action => action.Definition == "Create Post");

        Assert.Equal("POST", createPost.HttpType);
        Assert.Equal("Writing", createPost.ActionType);
        // Bosluklar atiliyor: "Create Post" -> "CreatePost".
        Assert.Equal("POST.Writing.CreatePost", createPost.Code);
    }

    [Fact]
    public void GetAuthorizeDefinitionEndpoints_ShouldReadHttpVerbFromActionAttribute()
    {
        var likeMenu = Menus.Single(menu => menu.Name == AuthorizeDefinitionConstants.Likes);

        Assert.Equal("POST", likeMenu.Actions.Single(action => action.Definition == "Create Like").HttpType);
        Assert.Equal("DELETE", likeMenu.Actions.Single(action => action.Definition == "Delete Like").HttpType);
        Assert.Equal("GET", likeMenu.Actions.Single(action => action.Definition == "Get Like Status").HttpType);
    }

    [Fact]
    public void GetAuthorizeDefinitionEndpoints_ShouldSupportPatchVerb()
    {
        var notificationMenu = Menus.Single(menu => menu.Name == AuthorizeDefinitionConstants.Notification);

        Assert.Equal("PATCH", notificationMenu.Actions.Single(action => action.Definition == "Mark All Notifications As Read").HttpType);
    }

    [Fact]
    public void GetAuthorizeDefinitionEndpoints_CodesShouldBeUniqueWithinMenu()
    {
        foreach (var menu in Menus)
        {
            var duplicateCodes = menu.Actions.GroupBy(action => action.Code).Where(group => group.Count() > 1).Select(group => group.Key).ToArray();

            Assert.True(duplicateCodes.Length == 0, $"{menu.Name} menusunde tekrar eden yetki kodu var: {string.Join(", ", duplicateCodes)}");
        }
    }

    [Fact]
    public void GetAuthorizeDefinitionEndpoints_ShouldSkipActionsWithoutAuthorizeDefinition()
    {
        // AuthController.Login gibi anonim uclar hicbir menuye girmemeli.
        var allDefinitions = Menus.SelectMany(menu => menu.Actions).Select(action => action.Definition).ToArray();

        Assert.DoesNotContain("Login", allDefinitions);
        Assert.DoesNotContain("Register", allDefinitions);
    }

    [Fact]
    public void GetAuthorizeDefinitionEndpoints_UnknownAssembly_ShouldNotThrowAndReturnNoMenus()
    {
        // Controller icermeyen bir assembly icin bos liste donmeli.
        var menus = new ApplicationService().GetAuthorizeDefinitionEndpoints(typeof(string));

        Assert.Empty(menus);
    }

    [Fact]
    public void GetAuthorizeDefinitionEndpoints_ShouldCoverEveryAnnotatedControllerAction()
    {
        var annotatedActionCount = typeof(PostController).Assembly
            .GetTypes()
            .Where(type => type.IsAssignableTo(typeof(Microsoft.AspNetCore.Mvc.ControllerBase)))
            .SelectMany(type => type.GetMethods())
            .Count(method => method.IsDefined(typeof(Application.Common.CustomAttrributes.AuthorizeDefinitionAttribute), true));

        Assert.Equal(annotatedActionCount, Menus.Sum(menu => menu.Actions.Count));
    }
}
