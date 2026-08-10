using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Bookmarks.Commands.Delete
{
    public class DeleteBookmarksCommandHandler : IRequestHandler<DeleteBookmarksCommand, DeleteBookmarksCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBookmarksCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DeleteBookmarksCommandResponse> Handle(DeleteBookmarksCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _unitOfWork.BookmarkRepository.DeleteByUserAndPostAsync(request.UserId, request.PostId, cancellationToken);

            var message = deleted ? "Yer işareti başarıyla silindi." : "Paylaşım yer işaretlerinizde bulunmuyor.";

            return new DeleteBookmarksCommandResponse(Message: message);
        }
    }
}
