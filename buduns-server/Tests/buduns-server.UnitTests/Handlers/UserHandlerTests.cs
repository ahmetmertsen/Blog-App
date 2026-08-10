using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos.User;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Features.Users.Commands.AssignRoleToUser;
using buduns_server.Application.Features.Users.Queries.GetAll;
using buduns_server.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace buduns_server.UnitTests.Handlers;

public class UserHandlerTests
{
    [Fact]
    public async Task AssignRole_Handle_ShouldPassActorTargetAndRolesToService()
    {
        var userService = Substitute.For<IUserService>();
        var handler = new AssignRoleToUserCommandHandler(
            userService,
            NullLogger<AssignRoleToUserCommandHandler>.Instance);
        var roles = new[] { "Admin", "User" };
        var command = new AssignRoleToUserCommand
        {
            UserId = 3,
            TargetUserId = 8,
            Roles = roles
        };

        var response = await handler.Handle(command, CancellationToken.None);

        await userService.Received(1).AssignRoleToUserAsync(3, 8, roles, CancellationToken.None);
    }

    [Fact]
    public async Task AssignRole_ServiceThrows_ShouldPropagateException()
    {
        var userService = Substitute.For<IUserService>();
        userService.AssignRoleToUserAsync(3, 8, Arg.Any<string[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new BadRequestException("invalid role")));
        var handler = new AssignRoleToUserCommandHandler(
            userService,
            NullLogger<AssignRoleToUserCommandHandler>.Instance);

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new AssignRoleToUserCommand
        {
            UserId = 3,
            TargetUserId = 8,
            Roles = new[] { "Unknown" }
        }, CancellationToken.None));
    }

    [Fact]
    public async Task GetAllUsers_Handle_ShouldPassAllFiltersAndBuildPagedResponse()
    {
        var userService = Substitute.For<IUserService>();
        var items = new List<AdminUserDto>
        {
            new() { Id = 1, UserName = "admin", FullName = "Admin User" }
        };
        userService.GetPagedUsersAsync(2, 25, "admin", UserStatus.Active, true, Arg.Any<CancellationToken>())
            .Returns((items, 51));
        var handler = new GetAllUsersQueryHandler(userService);
        var query = new GetAllUsersQuery
        {
            Page = 2,
            Size = 25,
            Search = "admin",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Same(items, result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(25, result.Size);
        Assert.Equal(51, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        await userService.Received(1).GetPagedUsersAsync(2, 25, "admin", UserStatus.Active, true, CancellationToken.None);
    }
}
