using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Bookmarks.Commands.Create
{
    public record CreateBookmarksCommandResponse(string Message, int BookmarkId, bool AlreadyBookmarked)
    {
    }
}
