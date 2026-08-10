using buduns_server.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Auth.Login
{
    public record LoginUserCommandResponse(string Message, Token Token)
    {
    }
}
