using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Likes.Commands.Delete
{
    public record DeleteLikesCommandResponse(bool Succeeded, string Message)
    {
    }
}
