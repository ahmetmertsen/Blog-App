using System.Reflection;
using System.Security.Claims;
using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace buduns_server.UnitTests.Filters;

public class RolePermissionFilterTests
{
    [Fact]
    public async Task Filter_ActionWithoutAuthorizeDefinition_ShouldContinue()
    {
        var permissionService = Substitute.For<IEndpointPermissionService>();
        var filter = CreateFilter(permissionService);
        var context = CreateContext(nameof(TestController.PublicAction), CreatePrincipal(1));
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        Assert.True(nextCalled);
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task Filter_AdminUser_ShouldBypassPermissionService()
    {
        var permissionService = Substitute.For<IEndpointPermissionService>();
        var filter = CreateFilter(permissionService);
        var context = CreateContext(nameof(TestController.ProtectedAction), CreatePrincipal(1, RoleConstants.Admin));
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        Assert.True(nextCalled);
        await permissionService.DidNotReceiveWithAnyArgs().HasAccessAsync(default, default!, default!);
    }

    [Fact]
    public async Task Filter_InvalidUserIdentifier_ShouldReturn401()
    {
        var permissionService = Substitute.For<IEndpointPermissionService>();
        var filter = CreateFilter(permissionService);
        var context = CreateContext(nameof(TestController.ProtectedAction), CreatePrincipal(null));

        await filter.OnActionExecutionAsync(context, CreateNext(context));

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public async Task Filter_UserWithoutPermission_ShouldReturn403()
    {
        var permissionService = Substitute.For<IEndpointPermissionService>();
        permissionService.HasAccessAsync(7, "POST.Writing.CreatePost", Arg.Any<IReadOnlyList<string>>()).ReturnsForAnyArgs(false);
        var filter = CreateFilter(permissionService);
        var context = CreateContext(nameof(TestController.ProtectedAction), CreatePrincipal(7));

        await filter.OnActionExecutionAsync(context, CreateNext(context));

        Assert.IsType<ForbidResult>(context.Result);
    }

    [Fact]
    public async Task Filter_UserWithPermission_ShouldContinueAndUseExpectedPermissionCode()
    {
        var permissionService = Substitute.For<IEndpointPermissionService>();
        permissionService.HasAccessAsync(default, default!, default!).ReturnsForAnyArgs(true);
        var filter = CreateFilter(permissionService);
        var context = CreateContext(nameof(TestController.ProtectedAction), CreatePrincipal(7));
        var nextCalled = false;

        await filter.OnActionExecutionAsync(context, CreateNext(context, () => nextCalled = true));

        Assert.True(nextCalled);
        await permissionService.Received(1).HasAccessAsync(7, "POST.Writing.CreatePost", Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Endpoint kaydi bulunamadiginda karari verecek olan liste bu; filtre
    /// bunu attribute'taki seviyeden turetip servise gecirmezse, kaydi silinen
    /// bir uc sessizce herkese kapanir.
    /// </summary>
    [Fact]
    public async Task Filter_ShouldPassDefaultRolesOfDeclaredAccessLevel()
    {
        var permissionService = Substitute.For<IEndpointPermissionService>();
        permissionService.HasAccessAsync(default, default!, default!).ReturnsForAnyArgs(true);
        var filter = CreateFilter(permissionService);
        var context = CreateContext(nameof(TestController.ProtectedAction), CreatePrincipal(7));

        await filter.OnActionExecutionAsync(context, CreateNext(context));

        await permissionService.Received(1).HasAccessAsync(
            7,
            "POST.Writing.CreatePost",
            Arg.Is<IReadOnlyList<string>>(roles => roles.SequenceEqual(new[] { RoleConstants.User, RoleConstants.Moderator })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Filter_ActionWithoutAccessLevel_ShouldPassNoDefaultRoles()
    {
        var permissionService = Substitute.For<IEndpointPermissionService>();
        permissionService.HasAccessAsync(default, default!, default!).ReturnsForAnyArgs(true);
        var filter = CreateFilter(permissionService);
        var context = CreateContext(nameof(TestController.AdminOnlyAction), CreatePrincipal(7));

        await filter.OnActionExecutionAsync(context, CreateNext(context));

        await permissionService.Received(1).HasAccessAsync(
            7,
            "POST.Writing.AssignRoleEndpoint",
            Arg.Is<IReadOnlyList<string>>(roles => roles.Count == 0),
            Arg.Any<CancellationToken>());
    }

    private static RolePermissionFilter CreateFilter(IEndpointPermissionService permissionService) =>
        new(permissionService, NullLogger<RolePermissionFilter>.Instance);

    private static ActionExecutingContext CreateContext(string methodName, ClaimsPrincipal user)
    {
        var method = typeof(TestController).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!;
        var descriptor = new ControllerActionDescriptor
        {
            MethodInfo = method,
            ControllerName = nameof(TestController),
            ActionName = methodName
        };
        var httpContext = new DefaultHttpContext { User = user };
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor, new ModelStateDictionary());
        return new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new TestController());
    }

    private static ActionExecutionDelegate CreateNext(ActionExecutingContext context, Action? callback = null) => () =>
    {
        callback?.Invoke();
        return Task.FromResult(new ActionExecutedContext(context, new List<IFilterMetadata>(), context.Controller));
    };

    private static ClaimsPrincipal CreatePrincipal(int? userId, string? role = null)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        }
        if (role != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
    }

    private sealed class TestController
    {
        public void PublicAction()
        {
        }

        [HttpPost]
        [AuthorizeDefinition(Menu = "Posts", ActionType = ActionType.Writing, Definition = "Create Post", AccessLevel = EndpointAccessLevel.Member)]
        public void ProtectedAction()
        {
        }

        [HttpPost]
        [AuthorizeDefinition(Menu = "Authorization Endpoints", ActionType = ActionType.Writing, Definition = "Assign Role Endpoint")]
        public void AdminOnlyAction()
        {
        }
    }
}
