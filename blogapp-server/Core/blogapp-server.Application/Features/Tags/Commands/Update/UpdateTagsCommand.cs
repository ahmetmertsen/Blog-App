using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Tags.Commands.Update
{
    public record UpdateTagsCommand(int Id, string Name) : IRequest<UpdateTagsCommandResponse>
    {
    }
}
