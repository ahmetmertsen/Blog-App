using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Bookmarks.Queries.GetStatus
{
    public class GetBookmarkStatusQueryHandler : IRequestHandler<GetBookmarkStatusQuery, GetBookmarkStatusQueryResponse>
    {
        private readonly IBookmarkRepository _bookmarkRepository;

        public GetBookmarkStatusQueryHandler(IBookmarkRepository bookmarkRepository)
        {
            _bookmarkRepository = bookmarkRepository;
        }

        public async Task<GetBookmarkStatusQueryResponse> Handle(GetBookmarkStatusQuery request, CancellationToken cancellationToken)
        {
            var bookmark = await _bookmarkRepository.GetByUserAndPostAsync(request.UserId, request.PostId, cancellationToken);

            return new GetBookmarkStatusQueryResponse(IsBookmarked: bookmark != null, BookmarkId: bookmark?.Id);
        }
    }
}
