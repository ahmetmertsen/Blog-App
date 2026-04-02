using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.ChangeEmail
{
    public record ChangeEmailCommandResponse(bool Succeeded, string Message)
    {
    }
}
