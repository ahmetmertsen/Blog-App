using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Delete
{
    public record DeleteUsersCommand(int userId) : IRequest<DeleteUsersCommandResponse>
    {
    }
}
