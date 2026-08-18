using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Bookmarks.Commands.Delete
{
    public class DeleteBookmarksCommandHandler : IRequestHandler<DeleteBookmarksCommand, DeleteBookmarksCommandResponse>
    {
        private readonly IBookmarkRepository _bookmarkRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBookmarksCommandHandler(IBookmarkRepository bookmarkRepository, IUnitOfWork unitOfWork)
        {
            _bookmarkRepository = bookmarkRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<DeleteBookmarksCommandResponse> Handle(DeleteBookmarksCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _bookmarkRepository.DeleteByUserAndPostAsync(request.UserId, request.PostId, cancellationToken);

            if (deleted)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var message = deleted ? "Yer işareti başarıyla silindi." : "Paylaşım yer işaretlerinizde bulunmuyor.";

            return new DeleteBookmarksCommandResponse(Message: message);
        }
    }
}
