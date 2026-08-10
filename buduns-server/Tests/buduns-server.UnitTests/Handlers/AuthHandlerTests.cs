using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos;
using buduns_server.Application.Dtos.Auth;
using buduns_server.Application.Dtos.Role;
using buduns_server.Application.Dtos.User;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Features.Auth.ChangeEmail;
using buduns_server.Application.Features.Auth.ForgotPassword;
using buduns_server.Application.Features.Auth.GetSessions;
using buduns_server.Application.Features.Auth.Login;
using buduns_server.Application.Features.Auth.Logout;
using buduns_server.Application.Features.Auth.LogoutAll;
using buduns_server.Application.Features.Auth.MailVerify;
using buduns_server.Application.Features.Auth.RefreshTokenLogin;
using buduns_server.Application.Features.Auth.Register;
using buduns_server.Application.Features.Auth.RevokeSession;
using buduns_server.Application.Features.AuthorizationEndpoint.Commands.AssignRoleEndpoint;
using buduns_server.Application.Features.AuthorizationEndpoint.Queries.GetRolesToEndpoint;
using buduns_server.Application.Features.Roles.Queries.GetAll;
using buduns_server.Application.Features.Roles.Queries.GetAllByUsername;
using buduns_server.Application.Features.Roles.Queries.GetById;
using buduns_server.Application.Features.Users.Commands.Update.UpdateEmail;
using buduns_server.Application.Features.Users.Commands.Update.UpdateMailVerify;
using buduns_server.Application.Features.Users.Commands.Update.UpdatePassword;
using buduns_server.Application.Features.Users.Commands.Update.UpdateProfile;
using buduns_server.Application.Features.Users.Queries.GetById;
using buduns_server.Application.Features.Users.Queries.GetByUsername;
using buduns_server.Application.Features.Users.Queries.GetRolesToUser;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace buduns_server.UnitTests.Handlers;

public class AuthHandlerTests
{
    [Fact]
    public async Task Login_ShouldReturnTokenFromService()
    {
        var authService = Substitute.For<IAuthService>();
        var token = new Token { AccessToken = "access", RefreshToken = "refresh", SessionId = Guid.NewGuid() };
        authService.LoginAsync("ahmet", "secret", Arg.Any<CancellationToken>()).Returns(token);
        var handler = new LoginUserCommandHandler(authService);

        var response = await handler.Handle(new LoginUserCommand("ahmet", "secret"), CancellationToken.None);

        Assert.Same(token, response.Token);
    }

    [Fact]
    public async Task Login_ServiceRejectsCredentials_ShouldPropagateUnauthorized()
    {
        var authService = Substitute.For<IAuthService>();
        authService.LoginAsync("ahmet", "wrong", Arg.Any<CancellationToken>()).Returns<Token>(_ => throw new UnauthorizedAccesException("hatali"));
        var handler = new LoginUserCommandHandler(authService);

        await Assert.ThrowsAsync<UnauthorizedAccesException>(() => handler.Handle(new LoginUserCommand("ahmet", "wrong"), CancellationToken.None));
    }

    [Fact]
    public async Task RefreshTokenLogin_ShouldReturnRotatedToken()
    {
        var authService = Substitute.For<IAuthService>();
        var token = new Token { AccessToken = "yeni", RefreshToken = "yeni-refresh" };
        authService.RefreshTokenLoginAsync("eski-refresh", Arg.Any<CancellationToken>()).Returns(token);
        var handler = new RefreshTokenLoginCommandHandler(authService);

        var response = await handler.Handle(new RefreshTokenLoginCommand { RefreshToken = "eski-refresh" }, CancellationToken.None);

        Assert.Same(token, response.Token);
    }

    [Fact]
    public async Task RefreshTokenLogin_InvalidToken_ShouldPropagateException()
    {
        var authService = Substitute.For<IAuthService>();
        authService.RefreshTokenLoginAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<Token>(_ => throw new InvalidRefreshTokenException("gecersiz"));
        var handler = new RefreshTokenLoginCommandHandler(authService);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() => handler.Handle(new RefreshTokenLoginCommand { RefreshToken = "x" }, CancellationToken.None));
    }

    [Fact]
    public async Task ForgotPassword_ShouldForwardIdentifierAndReturnNeutralResponse()
    {
        var authService = Substitute.For<IAuthService>();
        authService.ForgotPasswordResetAsync(Arg.Any<ForgotPasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ForgotPasswordResponse { Message = "Mail adresi dogru ise kod gonderildi." });
        var handler = new ForgotPasswordCommandHandler(authService);

        var response = await handler.Handle(new ForgotPasswordCommand { EmailOrUsername = "ahmet@test.com" }, CancellationToken.None);

        await authService.Received(1).ForgotPasswordResetAsync(Arg.Is<ForgotPasswordRequest>(request => request.EmailOrUsername == "ahmet@test.com"), CancellationToken.None);
    }

    [Fact]
    public async Task MailVerify_ShouldForwardCurrentUserId()
    {
        var authService = Substitute.For<IAuthService>();
        authService.MailVerifyAsync(Arg.Any<MailVerifyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new MailVerifyResponse { Message = "gonderildi" });
        var handler = new MailVerifyCommandHandler(authService);

        var response = await handler.Handle(new MailVerifyCommand { UserId = 9 }, CancellationToken.None);

        await authService.Received(1).MailVerifyAsync(Arg.Is<MailVerifyRequest>(request => request.UserId == 9), CancellationToken.None);
    }

    [Fact]
    public async Task ChangeEmail_ShouldForwardUserAndNewEmail()
    {
        var authService = Substitute.For<IAuthService>();
        authService.ChangeEmailAsync(Arg.Any<ChangeEmailRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChangeEmailResponse { Message = "kodlar gonderildi" });
        var handler = new ChangeEmailCommandHandler(authService);

        var response = await handler.Handle(new ChangeEmailCommand { UserId = 9, NewEmail = "yeni@test.com" }, CancellationToken.None);

        await authService.Received(1).ChangeEmailAsync(Arg.Is<ChangeEmailRequest>(request => request.UserId == 9 && request.NewEmail == "yeni@test.com"), CancellationToken.None);
    }

    [Fact]
    public async Task Logout_ShouldRevokeOnlyCurrentSession()
    {
        var sessionService = Substitute.For<IAuthSessionService>();
        var sessionId = Guid.NewGuid();
        var handler = new LogoutCommandHandler(sessionService);

        var response = await handler.Handle(new LogoutCommand { UserId = 9, CurrentSessionId = sessionId }, CancellationToken.None);

        await sessionService.Received(1).RevokeSessionAsync(9, sessionId, "User logout", CancellationToken.None);
        await sessionService.DidNotReceiveWithAnyArgs().RevokeAllSessionsAsync(default, default!, default);
    }

    [Fact]
    public async Task LogoutAll_ShouldRevokeEverySession()
    {
        var sessionService = Substitute.For<IAuthSessionService>();
        var handler = new LogoutAllCommandHandler(sessionService);

        var response = await handler.Handle(new LogoutAllCommand { UserId = 9 }, CancellationToken.None);

        await sessionService.Received(1).RevokeAllSessionsAsync(9, "User logout from all sessions", CancellationToken.None);
    }

    [Fact]
    public async Task RevokeSession_ExistingSession_ShouldSucceed()
    {
        var sessionService = Substitute.For<IAuthSessionService>();
        var sessionId = Guid.NewGuid();
        sessionService.RevokeSessionAsync(9, sessionId, "Revoked by user", Arg.Any<CancellationToken>()).Returns(true);
        var handler = new RevokeSessionCommandHandler(sessionService);

        var response = await handler.Handle(new RevokeSessionCommand { UserId = 9, SessionId = sessionId }, CancellationToken.None);

    }

    [Fact]
    public async Task RevokeSession_ForeignOrMissingSession_ShouldThrowNotFound()
    {
        var sessionService = Substitute.For<IAuthSessionService>();
        sessionService.RevokeSessionAsync(Arg.Any<int>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new RevokeSessionCommandHandler(sessionService);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new RevokeSessionCommand { UserId = 9, SessionId = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task GetSessions_ShouldPassCurrentSessionSoItCanBeFlagged()
    {
        var sessionService = Substitute.For<IAuthSessionService>();
        var sessionId = Guid.NewGuid();
        var sessions = new List<AuthSessionDto> { new() { Id = sessionId, IsCurrent = true } };
        sessionService.GetActiveSessionsAsync(9, sessionId, Arg.Any<CancellationToken>()).Returns(sessions);
        var handler = new GetAuthSessionsQueryHandler(sessionService);

        var response = await handler.Handle(new GetAuthSessionsQuery { UserId = 9, CurrentSessionId = sessionId }, CancellationToken.None);

        Assert.Same(sessions, response.Sessions);
    }

    [Fact]
    public async Task Register_ShouldMapCommandToRequestDto()
    {
        var userService = Substitute.For<IUserService>();
        userService.RegisterAsync(Arg.Any<RegisterUserRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new RegisterUserResponseDto { Message = "kaydedildi" });
        var handler = new RegisterUserCommandHandler(userService);

        var response = await handler.Handle(new RegisterUserCommand("ahmet", "Ahmet Mert", "ahmet@test.com", "Secret123!"), CancellationToken.None);

        await userService.Received(1).RegisterAsync(
            Arg.Is<RegisterUserRequestDto>(dto => dto.UserName == "ahmet" && dto.FullName == "Ahmet Mert" && dto.Email == "ahmet@test.com" && dto.Password == "Secret123!"),
            CancellationToken.None);
    }

    [Fact]
    public async Task Register_ShouldPassServiceMessageThrough()
    {
        var userService = Substitute.For<IUserService>();
        userService.RegisterAsync(Arg.Any<RegisterUserRequestDto>(), Arg.Any<CancellationToken>())
            .Returns(new RegisterUserResponseDto { Message = "kaydedildi ama dogrulama postasi gonderilemedi" });
        var handler = new RegisterUserCommandHandler(userService);

        var response = await handler.Handle(new RegisterUserCommand("ahmet", "Ahmet", "a@test.com", "123456"), CancellationToken.None);

        Assert.Equal("kaydedildi ama dogrulama postasi gonderilemedi", response.Message);
    }

    [Fact]
    public async Task UpdatePassword_ShouldForwardEveryField()
    {
        var userService = Substitute.For<IUserService>();
        userService.UpdatePasswordAsync(Arg.Any<UpdateUserPasswordRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateUserPasswordResponse { Message = "guncellendi" });
        var handler = new UpdateUserPasswordCommandHandler(userService);

        var response = await handler.Handle(new UpdateUserPasswordCommand
        {
            EmailOrUsername = "ahmet",
            VerificationCode = "123456",
            newPassword = "yeni123",
            newPasswordConfirmed = "yeni123"
        }, CancellationToken.None);

        await userService.Received(1).UpdatePasswordAsync(
            Arg.Is<UpdateUserPasswordRequest>(request => request.EmailOrUsername == "ahmet" && request.VerificationCode == "123456" && request.newPassword == "yeni123" && request.newPasswordConfirmed == "yeni123"),
            CancellationToken.None);
    }

    [Fact]
    public async Task UpdateMailVerify_ShouldForwardCurrentUserAndCode()
    {
        var userService = Substitute.For<IUserService>();
        userService.UpdateUserMailVerify(Arg.Any<UpdateUserMailVerifyRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateUserMailVerifyResponse { Message = "dogrulandi" });
        var handler = new UpdateUserMailVerifyCommandHandler(userService);

        var response = await handler.Handle(new UpdateUserMailVerifyCommand { UserId = 9, VerificationCode = "123456" }, CancellationToken.None);

        await userService.Received(1).UpdateUserMailVerify(Arg.Is<UpdateUserMailVerifyRequest>(request => request.UserId == 9 && request.VerificationCode == "123456"), CancellationToken.None);
    }

    [Fact]
    public async Task UpdateEmail_ShouldForwardBothVerificationCodes()
    {
        var userService = Substitute.For<IUserService>();
        userService.UpdateUserEmailAsync(Arg.Any<UpdateUserEmailRequest>(), Arg.Any<CancellationToken>())
            .Returns(new UpdateUserEmailResponse { Message = "guncellendi" });
        var handler = new UpdateUserEmailCommandHandler(userService);

        var response = await handler.Handle(new UpdateUserEmailCommand
        {
            UserId = 9,
            OldEmailVerificationCode = "111111",
            NewEmailVerificationCode = "222222",
            NewEmail = "yeni@test.com"
        }, CancellationToken.None);

        await userService.Received(1).UpdateUserEmailAsync(
            Arg.Is<UpdateUserEmailRequest>(request => request.UserId == 9 && request.OldEmailVerificationCode == "111111" && request.NewEmailVerificationCode == "222222" && request.NewEmail == "yeni@test.com"),
            CancellationToken.None);
    }

    [Fact]
    public async Task UpdateProfile_ShouldForwardProfileFields()
    {
        var userService = Substitute.For<IUserService>();
        userService.UpdateUserProfile(Arg.Any<UpdateUserProfileRequest>())
            .Returns(new UpdateUserProfileResponse { Message = "guncellendi" });
        var handler = new UpdateUserProfileCommandHandler(userService);

        var response = await handler.Handle(new UpdateUserProfileCommand { UserId = 9, FullName = "Ahmet Mert", Bio = "bio", ImageUrl = "http://img" }, CancellationToken.None);

        await userService.Received(1).UpdateUserProfile(Arg.Is<UpdateUserProfileRequest>(request => request.UserId == 9 && request.FullName == "Ahmet Mert" && request.Bio == "bio" && request.ImageUrl == "http://img"));
    }

    [Fact]
    public async Task GetUserById_ShouldReturnServiceResult()
    {
        var userService = Substitute.For<IUserService>();
        var dto = new UserDto { Id = 9, UserName = "ahmet", FullName = "Ahmet" };
        userService.GetUserById(9).Returns(dto);
        var handler = new GetUserByIdQueryHandler(userService);

        Assert.Same(dto, await handler.Handle(new GetUserByIdQuery { UserId = 9 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetUserByUsername_ShouldReturnServiceResult()
    {
        var userService = Substitute.For<IUserService>();
        var dto = new UserDto { Id = 9, UserName = "ahmet", FullName = "Ahmet" };
        userService.GetUserByUserName("ahmet").Returns(dto);
        var handler = new GetUserByUsernameQueryHandler(userService);

        Assert.Same(dto, await handler.Handle(new GetUserByUsernameQuery { UserName = "ahmet" }, CancellationToken.None));
    }

    [Fact]
    public async Task GetRolesToUser_ShouldEchoUserIdAlongsideRoles()
    {
        var userService = Substitute.For<IUserService>();
        userService.GetRolesToUserAsync(9).Returns(new[] { "Admin", "User" });
        var handler = new GetRolesToUserQueryHandler(userService);

        var response = await handler.Handle(new GetRolesToUserQuery { UserId = 9 }, CancellationToken.None);

        Assert.Equal(9, response.UserId);
        Assert.Equal(new[] { "Admin", "User" }, response.Roles);
    }

    [Fact]
    public async Task GetAllRoles_ShouldReturnServiceResult()
    {
        var roleService = Substitute.For<IRoleService>();
        var roles = new List<RoleDto> { new() { Id = 1, Name = "Admin" } };
        roleService.GetAllRoles(Arg.Any<CancellationToken>()).Returns(roles);
        var handler = new GetAllRolesQueryHandler(roleService);

        Assert.Same(roles, await handler.Handle(new GetAllRolesQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task GetRoleById_ShouldReturnServiceResult()
    {
        var roleService = Substitute.For<IRoleService>();
        var role = new RoleDto { Id = 3, Name = "Moderator" };
        roleService.GetRoleById(3, Arg.Any<CancellationToken>()).Returns(role);
        var handler = new GetRoleByIdQueryHandler(roleService);

        Assert.Same(role, await handler.Handle(new GetRoleByIdQuery { Id = 3 }, CancellationToken.None));
    }

    [Fact]
    public async Task GetRolesByUsername_ShouldReturnServiceResult()
    {
        var roleService = Substitute.For<IRoleService>();
        var roles = new List<RoleDto> { new() { Id = 1, Name = "User" } };
        roleService.GetRolesByUsername("ahmet", Arg.Any<CancellationToken>()).Returns(roles);
        var handler = new GetRolesByUsernameQueryHandler(roleService);

        Assert.Same(roles, await handler.Handle(new GetRolesByUsernameQuery { UserName = "ahmet" }, CancellationToken.None));
    }

    [Fact]
    public async Task AssignRoleEndpoint_ShouldForwardRolesMenuCodeAndType()
    {
        var endpointService = Substitute.For<IAuthorizationEndpointService>();
        var handler = new AssignRoleEndpointCommandHandler(endpointService, NullLogger<AssignRoleEndpointCommandHandler>.Instance);
        var roles = new[] { "Moderator" };

        var response = await handler.Handle(new AssignRoleEndpointCommand { Roles = roles, Menu = "Posts", Code = "POST.Writing.CreatePost", Type = typeof(WebAPI.Program) }, CancellationToken.None);

        await endpointService.Received(1).AssignRoleEndpointAsync(roles, "Posts", "POST.Writing.CreatePost", typeof(WebAPI.Program));
    }

    [Fact]
    public async Task AssignRoleEndpoint_WithoutType_ShouldThrowInvalidOperation()
    {
        // Type controller tarafindan atanir; bos olmasi istemci degil kod hatasidir.
        var handler = new AssignRoleEndpointCommandHandler(Substitute.For<IAuthorizationEndpointService>(), NullLogger<AssignRoleEndpointCommandHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new AssignRoleEndpointCommand { Roles = new[] { "Moderator" }, Menu = "Posts", Code = "POST.Writing.CreatePost", Type = null }, CancellationToken.None));
    }

    [Fact]
    public async Task GetRolesToEndpoint_ShouldReturnServiceRoles()
    {
        var endpointService = Substitute.For<IAuthorizationEndpointService>();
        endpointService.GetRolesToEndpoint("POST.Writing.CreatePost", "Posts").Returns(new List<string> { "Moderator" });
        var handler = new GetRolesToEndpointQueryHandler(endpointService);

        var response = await handler.Handle(new GetRolesToEndpointQuery { Code = "POST.Writing.CreatePost", Menu = "Posts" }, CancellationToken.None);

        Assert.Equal(new[] { "Moderator" }, response.Roles);
    }
}
