using System.Security.Claims;
using buduns_server.Application.Common.Behaviors;
using buduns_server.Application.Common.Interfaces;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace buduns_server.UnitTests.Behaviors;

public class AccountStatusBehaviorTests
{
    [Fact]
    public async Task Handle_ActiveAndVerifiedUser_ShouldContinue()
    {
        var user = CreateUser(UserStatus.Active, emailConfirmed: true);
        var (userRepository, unitOfWork) = CreateContext(user);
        var behavior = CreateBehavior<TestRequest>(userRepository, unitOfWork, authenticated: true);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_BannedUser_ShouldThrowForbidden()
    {
        var (userRepository, unitOfWork) = CreateContext(CreateUser(UserStatus.Banned, emailConfirmed: true));
        var behavior = CreateBehavior<TestRequest>(userRepository, unitOfWork, authenticated: true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActiveSuspension_ShouldThrowForbidden()
    {
        var user = CreateUser(UserStatus.Suspended, emailConfirmed: true);
        user.SuspendedUntil = DateTime.UtcNow.AddMinutes(10);
        var (userRepository, unitOfWork) = CreateContext(user);
        var behavior = CreateBehavior<TestRequest>(userRepository, unitOfWork, authenticated: true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExpiredSuspension_ShouldReactivateUser()
    {
        var user = CreateUser(UserStatus.Suspended, emailConfirmed: true);
        user.SuspendedUntil = DateTime.UtcNow.AddMinutes(-1);
        var (userRepository, unitOfWork) = CreateContext(user);
        var behavior = CreateBehavior<TestRequest>(userRepository, unitOfWork, authenticated: true);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Null(user.SuspendedUntil);
        userRepository.Received(1).Update(user);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnverifiedUser_ShouldRequireVerification()
    {
        var (userRepository, unitOfWork) = CreateContext(CreateUser(UserStatus.Active, emailConfirmed: false));
        var behavior = CreateBehavior<TestRequest>(userRepository, unitOfWork, authenticated: true);

        await Assert.ThrowsAsync<EmailVerificationRequiredException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllowUnverifiedRequest_ShouldContinue()
    {
        var (userRepository, unitOfWork) = CreateContext(CreateUser(UserStatus.Active, emailConfirmed: false));
        var behavior = CreateBehavior<AllowUnverifiedRequest>(userRepository, unitOfWork, authenticated: true);

        var result = await behavior.Handle(new AllowUnverifiedRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_AnonymousRequest_ShouldContinueWithoutUserLookup()
    {
        var (userRepository, unitOfWork) = CreateContext(CreateUser(UserStatus.Banned, emailConfirmed: false));
        var behavior = CreateBehavior<TestRequest>(userRepository, unitOfWork, authenticated: false);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("public"), CancellationToken.None);

        Assert.Equal("public", result);
        await userRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    private static AccountStatusBehavior<TRequest, string> CreateBehavior<TRequest>(IUserRepository userRepository, IUnitOfWork unitOfWork, bool authenticated)
        where TRequest : notnull
    {
        var claims = authenticated ? new[] { new Claim(ClaimTypes.NameIdentifier, "5") } : Array.Empty<Claim>();
        var identity = new ClaimsIdentity(claims, authenticated ? "TestAuthentication" : null);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new AccountStatusBehavior<TRequest, string>(new HttpContextAccessor { HttpContext = context }, userRepository, unitOfWork);
    }

    private static User CreateUser(UserStatus status, bool emailConfirmed) => new()
    {
        Id = 5,
        UserName = "test-user",
        FullName = "Test User",
        Status = status,
        EmailConfirmed = emailConfirmed
    };

    private static (IUserRepository UserRepository, IUnitOfWork UnitOfWork) CreateContext(User user)
    {
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return (userRepository, Substitute.For<IUnitOfWork>());
    }

    private sealed class TestRequest
    {
    }

    private sealed class AllowUnverifiedRequest : IAllowUnverifiedEmail
    {
    }
}
