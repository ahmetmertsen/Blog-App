using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Followers.Commands.Delete
{
    public record DeleteFollowersCommandResponse(bool Succeeded, string Message)
    {
    }
}
