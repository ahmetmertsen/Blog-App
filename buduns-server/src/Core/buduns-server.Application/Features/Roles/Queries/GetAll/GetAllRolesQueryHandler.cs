using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos.Role;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Roles.Queries.GetAll
{
    public class GetAllRolesQueryHandler : IRequestHandler<GetAllRolesQuery, List<RoleDto>>
    {
        private readonly IRoleService _roleService;

        public GetAllRolesQueryHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        public async Task<List<RoleDto>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _roleService.GetAllRoles(cancellationToken);
            return roles;
        }
    }
}
