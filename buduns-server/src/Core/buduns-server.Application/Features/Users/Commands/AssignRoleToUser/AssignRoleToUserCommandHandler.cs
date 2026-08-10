using buduns_server.Application.Abstractions.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Commands.AssignRoleToUser
{
    public class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand, AssignRoleToUserCommandResponse>
    {
        private readonly IUserService _userService;
        private readonly ILogger<AssignRoleToUserCommandHandler> _logger;

        public AssignRoleToUserCommandHandler(IUserService userService, ILogger<AssignRoleToUserCommandHandler> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task<AssignRoleToUserCommandResponse> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
        {
            await _userService.AssignRoleToUserAsync(request.UserId, request.TargetUserId, request.Roles, cancellationToken);

            _logger.LogInformation("Roles assigned to user. ActorUserId: {ActorUserId}, TargetUserId: {TargetUserId}, Roles: {Roles}, RoleCount: {RoleCount}", request.UserId, request.TargetUserId, request.Roles, request.Roles.Length);

            AssignRoleToUserCommandResponse response = new()
            {
                Message = "Kullanıcıya roller atandı."
            };
            return response;
        }
    }
}
