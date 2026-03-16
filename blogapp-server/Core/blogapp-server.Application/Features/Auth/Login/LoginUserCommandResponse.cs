using blogapp_server.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.Login
{
    public record LoginUserCommandResponse(bool Succeeded, string Message, Token Token)
    {
    }
}
