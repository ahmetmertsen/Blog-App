using buduns_server.Application.Features.Bookmarks.Commands.Create;
using buduns_server.Domain.Entities;

namespace buduns_server.Application.Mapping
{
    public static class BookmarkMappings
    {
        public static Bookmark ToEntity(this CreateBookmarksCommand command) => new()
        {
            PostId = command.PostId,
            UserId = command.UserId
        };
    }
}
