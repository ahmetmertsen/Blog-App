using System.Security.Claims;
using buduns_server.Application.Common.Behaviors;
using buduns_server.Application.Common.Interfaces;
using buduns_server.Application.Exceptions;
using Microsoft.AspNetCore.Http;

namespace buduns_server.UnitTests.Behaviors;

public class CurrentUserBehaviorTests
{
    [Fact]
    public async Task Handle_AuthenticatedUser_ShouldOverwriteRequestUserId()
    {
        var context = CreateContext(new Claim(ClaimTypes.NameIdentifier, "42"));
        var behavior = new CurrentUserBehavior<UserRequest, string>(new HttpContextAccessor { HttpContext = context });
        var request = new UserRequest { UserId = 999 };

        var result = await behavior.Handle(request, _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(42, request.UserId);
    }

    [Fact]
    public async Task Handle_AnonymousUser_ShouldThrowUnauthorized()
    {
        var context = new DefaultHttpContext();
        var behavior = new CurrentUserBehavior<UserRequest, string>(new HttpContextAccessor { HttpContext = context });

        await Assert.ThrowsAsync<UnauthorizedAccesException>(() =>
            behavior.Handle(new UserRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidUserIdClaim_ShouldThrowUnauthorized()
    {
        var context = CreateContext(new Claim(ClaimTypes.NameIdentifier, "not-an-id"));
        var behavior = new CurrentUserBehavior<UserRequest, string>(new HttpContextAccessor { HttpContext = context });

        await Assert.ThrowsAsync<UnauthorizedAccesException>(() =>
            behavior.Handle(new UserRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidSessionClaim_ShouldSetSessionId()
    {
        var sessionId = Guid.NewGuid();
        var context = CreateContext(
            new Claim(ClaimTypes.NameIdentifier, "5"),
            new Claim("sid", sessionId.ToString()));
        var request = new UserAndSessionRequest();
        var behavior = new CurrentUserBehavior<UserAndSessionRequest, string>(new HttpContextAccessor { HttpContext = context });

        await behavior.Handle(request, _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal(5, request.UserId);
        Assert.Equal(sessionId, request.CurrentSessionId);
    }

    [Fact]
    public async Task Handle_InvalidSessionClaim_ShouldThrowUnauthorized()
    {
        var context = CreateContext(
            new Claim(ClaimTypes.NameIdentifier, "5"),
            new Claim("sid", "invalid"));
        var behavior = new CurrentUserBehavior<UserAndSessionRequest, string>(new HttpContextAccessor { HttpContext = context });

        await Assert.ThrowsAsync<UnauthorizedAccesException>(() =>
            behavior.Handle(new UserAndSessionRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_RequestWithoutCurrentUserContract_ShouldNotRequireAuthentication()
    {
        var context = new DefaultHttpContext();
        var behavior = new CurrentUserBehavior<PublicRequest, string>(new HttpContextAccessor { HttpContext = context });

        var result = await behavior.Handle(new PublicRequest(), _ => Task.FromResult("public"), CancellationToken.None);

        Assert.Equal("public", result);
    }

    private static DefaultHttpContext CreateContext(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuthentication");
        return new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
    }

    private class UserRequest : ICurrentUserRequest
    {
        public int UserId { get; set; }
    }

    private sealed class UserAndSessionRequest : UserRequest, ICurrentSessionRequest
    {
        public Guid CurrentSessionId { get; set; }
    }

    private sealed class PublicRequest
    {
    }
}
