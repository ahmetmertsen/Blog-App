using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Commands.Update
{
    public record UpdatePostsCommandResponse(bool Succeeded, string Message)
    {
    }
}
