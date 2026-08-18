using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Bookmarks.Queries.GetBookmarks
{
    public class GetBookmarksQueryHandler : IRequestHandler<GetBookmarksQuery, PagedResponse<BookmarkDto>>
    {
        private readonly IBookmarkRepository _bookmarkRepository;

        public GetBookmarksQueryHandler(IBookmarkRepository bookmarkRepository)
        {
            _bookmarkRepository = bookmarkRepository;
        }

        public async Task<PagedResponse<BookmarkDto>> Handle(GetBookmarksQuery request, CancellationToken cancellationToken)
        {
            var result = await _bookmarkRepository.GetPagedByUserIdAsync(request.UserId, request.Page, request.Size, cancellationToken);

            return new PagedResponse<BookmarkDto>
            {
                Items = result.Items,
                Page = request.Page,
                Size = request.Size,
                TotalCount = result.TotalCount
            };
        }
    }
}
