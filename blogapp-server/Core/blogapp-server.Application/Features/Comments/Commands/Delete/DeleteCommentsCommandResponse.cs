using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Commands.Delete
{
    public record DeleteCommentsCommandResponse(bool Succeeded, string Message)
    {
    }
}
