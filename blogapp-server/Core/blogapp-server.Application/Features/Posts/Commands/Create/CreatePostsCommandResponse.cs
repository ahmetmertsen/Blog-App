using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Commands.Create
{
    public record CreatePostsCommandResponse(bool Succeeded, string Message) 
    {
    }
}
