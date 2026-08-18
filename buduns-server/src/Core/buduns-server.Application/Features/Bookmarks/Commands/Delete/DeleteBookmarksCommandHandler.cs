using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Bookmarks.Commands.Delete
{
    public class DeleteBookmarksCommandHandler : IRequestHandler<DeleteBookmarksCommand, DeleteBookmarksCommandResponse>
    {
        private readonly IBookmarkRepository _bookmarkRepository;

        public DeleteBookmarksCommandHandler(IBookmarkRepository bookmarkRepository)
        {
            _bookmarkRepository = bookmarkRepository;
        }

        public async Task<DeleteBookmarksCommandResponse> Handle(DeleteBookmarksCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _bookmarkRepository.DeleteByUserAndPostAsync(request.UserId, request.PostId, cancellationToken);

            var message = deleted ? "Yer işareti başarıyla silindi." : "Paylaşım yer işaretlerinizde bulunmuyor.";

            return new DeleteBookmarksCommandResponse(Message: message);
        }
    }
}
