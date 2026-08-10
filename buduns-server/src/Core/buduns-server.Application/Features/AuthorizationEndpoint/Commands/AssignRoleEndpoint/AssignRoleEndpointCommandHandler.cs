using buduns_server.Application.Abstractions.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.AuthorizationEndpoint.Commands.AssignRoleEndpoint
{
    public class AssignRoleEndpointCommandHandler : IRequestHandler<AssignRoleEndpointCommand, AssignRoleEndpointCommandResponse>
    {
        private readonly IAuthorizationEndpointService _authorizationEndpointService;
        private readonly ILogger<AssignRoleEndpointCommandHandler> _logger;

        public AssignRoleEndpointCommandHandler(IAuthorizationEndpointService authorizationEndpointService, ILogger<AssignRoleEndpointCommandHandler> logger)
        {
            _authorizationEndpointService = authorizationEndpointService;
            _logger = logger;
        }

        public async Task<AssignRoleEndpointCommandResponse> Handle(AssignRoleEndpointCommand request, CancellationToken cancellationToken)
        {
            // Type gelen govdeden degil, controller tarafindan atanir (JsonIgnore).
            // Bos olmasi istemci hatasi degil kod hatasidir.
            var type = request.Type ?? throw new InvalidOperationException($"{nameof(AssignRoleEndpointCommand)}.{nameof(request.Type)} atanmamis.");

            await _authorizationEndpointService.AssignRoleEndpointAsync(request.Roles, request.Menu, request.Code, type);

            _logger.LogInformation(
                "Roles assigned to endpoint. Menu: {Menu}, PermissionCode: {PermissionCode}, Roles: {Roles}, RoleCount: {RoleCount}",
                request.Menu,
                request.Code,
                request.Roles,
                request.Roles.Length);

            return new AssignRoleEndpointCommandResponse
            {
                Message = "Endpointe roller atandı."
            };
        }
    }
}
