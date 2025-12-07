using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Bookmarks.Commands.Create
{
    public record CreateBookmarksCommand(int UserId, int PostId) : IRequest<CreateBookmarksCommandResponse>
    {
    }
}
