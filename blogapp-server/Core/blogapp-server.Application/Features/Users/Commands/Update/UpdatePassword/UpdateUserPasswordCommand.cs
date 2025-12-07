using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdatePassword
{
    public record UpdateUserPasswordCommand(int Id, string Password) : IRequest<UpdateUserPasswordCommandResponse>
    {
    }
}
