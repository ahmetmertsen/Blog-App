using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Features.Roles.Commands.Create;
using blogapp_server.Application.Features.Roles.Commands.Update;
using blogapp_server.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Mapping
{
    public class RoleProfile : Profile
    {
        public RoleProfile() 
        {
            CreateMap<CreateRolesCommand, Role>();
            CreateMap<UpdateRolesCommand, Role>();

            CreateMap<Role, RoleDto>();
        }
    }
}
