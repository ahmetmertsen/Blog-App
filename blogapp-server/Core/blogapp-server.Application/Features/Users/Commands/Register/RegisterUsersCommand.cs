using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Register
{
    public record RegisterUsersCommand(string UserName, string FullName, string Email, string Password) : IRequest<RegisterUsersCommandResponse>
    {
    }
    
}
