using buduns_server.Application.Abstractions.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace buduns_server.Application.Features.Roles.Commands.Create
{
    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, CreateRoleCommandResponse>
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<CreateRoleCommandHandler> _logger;

        public CreateRoleCommandHandler(IRoleService roleService, ILogger<CreateRoleCommandHandler> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        public async Task<CreateRoleCommandResponse> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            await _roleService.CreateRole(request.Name, cancellationToken);
            _logger.LogInformation("Role created. RoleName: {RoleName}", request.Name.Trim());
            return new CreateRoleCommandResponse { Message = "Rol başarıyla eklendi." };
        }
    }
}
