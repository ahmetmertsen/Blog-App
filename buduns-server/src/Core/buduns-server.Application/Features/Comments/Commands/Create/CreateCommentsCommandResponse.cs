using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using buduns_server.Application.Dtos;

namespace buduns_server.Application.Features.Comments.Commands.Create
{
    public record CreateCommentsCommandResponse(string Message, CommentDto Comment)
    {
    }
}
