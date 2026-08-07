using buduns_server.Application.Dtos.Role;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Roles.Queries.GetAllByUsername
{
    public class GetRolesByUsernameQuery : IRequest<List<RoleDto>>
    {
        public string UserName { get; set; } = string.Empty;
    }
}
