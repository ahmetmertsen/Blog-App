using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.AuthorizationEndpoint.Commands.AssignRoleEndpoint
{
    public class AssignRoleEndpointCommand : IRequest<AssignRoleEndpointCommandResponse>, ITransactionalRequest
    {
        public string[] Roles { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Menu { get; set; } = null!;

        [JsonIgnore]
        public Type? Type { get; set; }
    }
}
