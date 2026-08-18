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
        var unitOfWork = CreateUnitOfWork(user);
        var behavior = CreateBehavior<TestRequest>(unitOfWork, authenticated: true);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_BannedUser_ShouldThrowForbidden()
    {
        var unitOfWork = CreateUnitOfWork(CreateUser(UserStatus.Banned, emailConfirmed: true));
        var behavior = CreateBehavior<TestRequest>(unitOfWork, authenticated: true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ActiveSuspension_ShouldThrowForbidden()
    {
        var user = CreateUser(UserStatus.Suspended, emailConfirmed: true);
        user.SuspendedUntil = DateTime.UtcNow.AddMinutes(10);
        var unitOfWork = CreateUnitOfWork(user);
        var behavior = CreateBehavior<TestRequest>(unitOfWork, authenticated: true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExpiredSuspension_ShouldReactivateUser()
    {
        var user = CreateUser(UserStatus.Suspended, emailConfirmed: true);
        user.SuspendedUntil = DateTime.UtcNow.AddMinutes(-1);
        var unitOfWork = CreateUnitOfWork(user);
        var behavior = CreateBehavior<TestRequest>(unitOfWork, authenticated: true);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Null(user.SuspendedUntil);
        unitOfWork.UserRepository.Received(1).Update(user);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnverifiedUser_ShouldRequireVerification()
    {
        var unitOfWork = CreateUnitOfWork(CreateUser(UserStatus.Active, emailConfirmed: false));
        var behavior = CreateBehavior<TestRequest>(unitOfWork, authenticated: true);

        await Assert.ThrowsAsync<EmailVerificationRequiredException>(() =>
            behavior.Handle(new TestRequest(), _ => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllowUnverifiedRequest_ShouldContinue()
    {
        var unitOfWork = CreateUnitOfWork(CreateUser(UserStatus.Active, emailConfirmed: false));
        var behavior = CreateBehavior<AllowUnverifiedRequest>(unitOfWork, authenticated: true);

        var result = await behavior.Handle(new AllowUnverifiedRequest(), _ => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_AnonymousRequest_ShouldContinueWithoutUserLookup()
    {
        var unitOfWork = CreateUnitOfWork(CreateUser(UserStatus.Banned, emailConfirmed: false));
        var behavior = CreateBehavior<TestRequest>(unitOfWork, authenticated: false);

        var result = await behavior.Handle(new TestRequest(), _ => Task.FromResult("public"), CancellationToken.None);

        Assert.Equal("public", result);
        await unitOfWork.UserRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    private static AccountStatusBehavior<TRequest, string> CreateBehavior<TRequest>(IUnitOfWork unitOfWork, bool authenticated)
        where TRequest : notnull
    {
        var claims = authenticated ? new[] { new Claim(ClaimTypes.NameIdentifier, "5") } : Array.Empty<Claim>();
        var identity = new ClaimsIdentity(claims, authenticated ? "TestAuthentication" : null);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        return new AccountStatusBehavior<TRequest, string>(new HttpContextAccessor { HttpContext = context }, unitOfWork);
    }

    private static User CreateUser(UserStatus status, bool emailConfirmed) => new()
    {
        Id = 5,
        UserName = "test-user",
        FullName = "Test User",
        Status = status,
        EmailConfirmed = emailConfirmed
    };

    private static IUnitOfWork CreateUnitOfWork(User user)
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.UserRepository.Returns(Substitute.For<IUserRepository>());
        unitOfWork.UserRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        return unitOfWork;
    }

    private sealed class TestRequest
    {
    }

    private sealed class AllowUnverifiedRequest : IAllowUnverifiedEmail
    {
    }
}
