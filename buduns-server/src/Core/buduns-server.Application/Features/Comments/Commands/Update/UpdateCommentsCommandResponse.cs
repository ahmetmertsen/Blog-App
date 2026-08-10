using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using buduns_server.Application.Dtos;

namespace buduns_server.Application.Features.Comments.Commands.Update
{
    public record UpdateCommentsCommandResponse(string Message, CommentDto Comment)
    {
    }
}
