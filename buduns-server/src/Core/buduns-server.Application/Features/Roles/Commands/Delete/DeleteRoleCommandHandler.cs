using buduns_server.Application.Abstractions.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace buduns_server.Application.Features.Roles.Commands.Delete
{
    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, DeleteRoleCommandResponse>
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<DeleteRoleCommandHandler> _logger;

        public DeleteRoleCommandHandler(IRoleService roleService, ILogger<DeleteRoleCommandHandler> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        public async Task<DeleteRoleCommandResponse> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            await _roleService.DeleteRole(request.Id, cancellationToken);
            _logger.LogInformation("Role deleted. RoleId: {RoleId}", request.Id);
            return new DeleteRoleCommandResponse { Message = "Rol başarıyla silindi." };
        }
    }
}
