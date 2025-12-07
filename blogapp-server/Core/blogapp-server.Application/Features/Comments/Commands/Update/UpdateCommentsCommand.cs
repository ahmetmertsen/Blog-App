using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Commands.Update
{
    public record UpdateCommentsCommand(int Id, string Content) : IRequest<UpdateCommentsCommandResponse>
    {
    }
}
