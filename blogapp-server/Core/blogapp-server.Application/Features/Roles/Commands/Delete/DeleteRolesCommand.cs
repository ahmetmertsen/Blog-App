using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Roles.Commands.Delete
{
    public record DeleteRolesCommand(int Id) : IRequest<DeleteRolesCommandResponse>
    {
    }
}
