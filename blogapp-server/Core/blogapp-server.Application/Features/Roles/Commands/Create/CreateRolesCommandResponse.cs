using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Roles.Commands.Create
{
    public record CreateRolesCommandResponse(bool Succeeded, string Message)
    {
    }
}
