using System.Security.Claims;
using buduns_server.Application.Common.Behaviors;
using buduns_server.Application.Common.Interfaces;
using buduns_server.Application.Exceptions;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace buduns_server.UnitTests.Behaviors;

public class AccountStatusBehaviorTests
{
    [Fact]
    public async Task Handle_ActiveAndVerifiedUser_ShouldContinue()
    {
        var user = CreateUser(UserStatus.Active, emailConfirmed: true);
        var userManager = CreateUserManager(user);
        var behavior = CreateBehavior<TestRequest>(userManager, authenticated: true);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_BannedUser_ShouldThrowForbidden()
    {
        var userManager = CreateUserManager(CreateUser(UserStatus.Banned, emailConfirmed: true));
        var behavior = CreateBehavior<TestRequest>(userManager, authenticated: true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActiveSuspension_ShouldThrowForbidden()
    {
        var user = CreateUser(UserStatus.Suspended, emailConfirmed: true);
        user.SuspendedUntil = DateTime.UtcNow.AddMinutes(10);
        var userManager = CreateUserManager(user);
        var behavior = CreateBehavior<TestRequest>(userManager, authenticated: true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExpiredSuspension_ShouldReactivateUser()
    {
        var user = CreateUser(UserStatus.Suspended, emailConfirmed: true);
        user.SuspendedUntil = DateTime.UtcNow.AddMinutes(-1);
        var userManager = CreateUserManager(user);
        userManager.UpdateAsync(user).Returns(IdentityResult.Success);
        var behavior = CreateBehavior<TestRequest>(userManager, authenticated: true);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Null(user.SuspendedUntil);
        await userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task Handle_UnverifiedUser_ShouldRequireVerification()
    {
        var userManager = CreateUserManager(CreateUser(UserStatus.Active, emailConfirmed: false));
        var behavior = CreateBehavior<TestRequest>(userManager, authenticated: true);

        await Assert.ThrowsAsync<EmailVerificationRequiredException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllowUnverifiedRequest_ShouldContinue()
    {
        var userManager = CreateUserManager(CreateUser(UserStatus.Active, emailConfirmed: false));
        var behavior = CreateBehavior<AllowUnverifiedRequest>(userManager, authenticated: true);

        var result = await behavior.Handle(new AllowUnverifiedRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_AnonymousRequest_ShouldContinueWithoutUserLookup()
    {
        var userManager = CreateUserManager(CreateUser(UserStatus.Banned, emailConfirmed: false));
        var behavior = CreateBehavior<TestRequest>(userManager, authenticated: false);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("public"), CancellationToken.None);

        Assert.Equal("public", result);
        await userManager.DidNotReceiveWithAnyArgs().FindByIdAsync(default!);
    }

    private static AccountStatusBehavior<TRequest, string> CreateBehavior<TRequest>(UserManager<User> userManager, bool authenticated)
        where TRequest : notnull
    {
        var claims = authenticated ? new[] { new Claim(ClaimTypes.NameIdentifier, "5") } : Array.Empty<Claim>();
        var identity = new ClaimsIdentity(claims, authenticated ? "TestAuthentication" : null);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new AccountStatusBehavior<TRequest, string>(new HttpContextAccessor { HttpContext = context }, userManager);
    }

    private static User CreateUser(UserStatus status, bool emailConfirmed) => new()
    {
        Id = 5,
        UserName = "test-user",
        FullName = "Test User",
        Status = status,
        EmailConfirmed = emailConfirmed
    };

    private static UserManager<User> CreateUserManager(User user)
    {
        var store = Substitute.For<IUserStore<User>>();
        var manager = Substitute.For<UserManager<User>>(
            store,
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);
        manager.FindByIdAsync(user.Id.ToString()).Returns(user);
        return manager;
    }

    private sealed class TestRequest
    {
    }

    private sealed class AllowUnverifiedRequest : IAllowUnverifiedEmail
    {
    }
}
