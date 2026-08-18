using MediatR;
using buduns_server.Application.Common.Interfaces;
using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Commands.AssignRoleToUser
{
    public class AssignRoleToUserCommand : IRequest<AssignRoleToUserCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        [JsonPropertyName("userId")]
        public int TargetUserId { get; set; }

        public string[] Roles { get; set; } = null!;
    }
}
