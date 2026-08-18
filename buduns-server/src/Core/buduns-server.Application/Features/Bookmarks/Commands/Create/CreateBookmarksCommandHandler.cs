using buduns_server.Application.Exceptions;
using buduns_server.Application.Mapping;
using buduns_server.Application.Repositories;
using buduns_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Bookmarks.Commands.Create
{
    public class CreateBookmarksCommandHandler : IRequestHandler<CreateBookmarksCommand, CreateBookmarksCommandResponse>
    {
        private readonly IBookmarkRepository _bookmarkRepository;
        private readonly IPostRepository _postRepository;

        public CreateBookmarksCommandHandler(IBookmarkRepository bookmarkRepository, IPostRepository postRepository)
        {
            _bookmarkRepository = bookmarkRepository;
            _postRepository = postRepository;
        }

        public async Task<CreateBookmarksCommandResponse> Handle(CreateBookmarksCommand request, CancellationToken cancellationToken)
        {
            var post = await _postRepository.GetByIdAsync(request.PostId);
            if (post == null)
            {
                throw new NotFoundException("Kaydedilecek paylaşım bulunamadı.");
            }

            var bookmark = request.ToEntity();
            bookmark.isDeleted = false;
            bookmark.CreatedAt = DateTime.UtcNow;
            bookmark.isActive = true;

            var result = await _bookmarkRepository.CreateIfNotExistsAsync(bookmark, cancellationToken);
            var message = result.Created ? "Yer işareti başarıyla eklendi." : "Paylaşım zaten yer işaretlerinizde bulunuyor.";

            return new CreateBookmarksCommandResponse(
                Message: message,
                BookmarkId: result.Bookmark.Id,
                AlreadyBookmarked: !result.Created);
        }
    }
}
