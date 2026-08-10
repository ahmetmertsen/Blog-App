using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Features.Roles.Commands.Create;
using buduns_server.Application.Features.Roles.Commands.Delete;
using buduns_server.Application.Features.Roles.Commands.Update;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace buduns_server.UnitTests.Handlers;

public class RoleHandlerTests
{
    [Fact]
    public async Task CreateRole_Handle_ShouldCallServiceAndReturnSuccess()
    {
        var service = Substitute.For<IRoleService>();
        var handler = new CreateRoleCommandHandler(service, NullLogger<CreateRoleCommandHandler>.Instance);

        var response = await handler.Handle(new CreateRoleCommand { Name = "Editor" }, CancellationToken.None);

        await service.Received(1).CreateRole("Editor", CancellationToken.None);
    }

    [Fact]
    public async Task UpdateRole_Handle_ShouldCallServiceAndReturnSuccess()
    {
        var service = Substitute.For<IRoleService>();
        var handler = new UpdateRoleCommandHandler(service, NullLogger<UpdateRoleCommandHandler>.Instance);

        var response = await handler.Handle(new UpdateRoleCommand { Id = 4, Name = "Reviewer" }, CancellationToken.None);

        await service.Received(1).UpdateRole(4, "Reviewer", CancellationToken.None);
    }

    [Fact]
    public async Task DeleteRole_Handle_ShouldCallServiceAndReturnSuccess()
    {
        var service = Substitute.For<IRoleService>();
        var handler = new DeleteRoleCommandHandler(service, NullLogger<DeleteRoleCommandHandler>.Instance);

        var response = await handler.Handle(new DeleteRoleCommand { Id = 7 }, CancellationToken.None);

        await service.Received(1).DeleteRole(7, CancellationToken.None);
    }

    [Fact]
    public async Task CreateRole_ServiceThrows_ShouldPropagateApplicationException()
    {
        var service = Substitute.For<IRoleService>();
        service.CreateRole("Admin", Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new BadRequestException("system role")));
        var handler = new CreateRoleCommandHandler(service, NullLogger<CreateRoleCommandHandler>.Instance);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new CreateRoleCommand { Name = "Admin" }, CancellationToken.None));
    }
}
