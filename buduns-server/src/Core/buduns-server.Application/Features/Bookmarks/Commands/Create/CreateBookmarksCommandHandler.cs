using AutoMapper;
using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateBookmarksCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CreateBookmarksCommandResponse> Handle(CreateBookmarksCommand request, CancellationToken cancellationToken)
        {
            var post = await _unitOfWork.PostRepository.GetByIdAsync(request.PostId);
            if (post == null)
            {
                throw new NotFoundException("Kaydedilecek paylaşım bulunamadı.");
            }

            var bookmark = _mapper.Map<Bookmark>(request);
            bookmark.isDeleted = false;
            bookmark.CreatedAt = DateTime.UtcNow;
            bookmark.isActive = true;

            var result = await _unitOfWork.BookmarkRepository.CreateIfNotExistsAsync(bookmark, cancellationToken);
            var message = result.Created ? "Yer işareti başarıyla eklendi." : "Paylaşım zaten yer işaretlerinizde bulunuyor.";

            return new CreateBookmarksCommandResponse(
                Succeeded: true,
                Message: message,
                BookmarkId: result.Bookmark.Id,
                AlreadyBookmarked: !result.Created);
        }
    }
}
