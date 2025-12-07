using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdateEmail
{
    public record UpdateUserEmailCommandResponse(bool Succeeded, string Message)
    {
    }
}
