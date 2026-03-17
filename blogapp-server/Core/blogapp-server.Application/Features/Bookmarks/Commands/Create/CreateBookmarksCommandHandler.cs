using AutoMapper;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Bookmarks.Commands.Create
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
            var bookmark = _mapper.Map<Bookmark>(request);
            bookmark.isDeleted = false;
            bookmark.CreatedAt = DateTime.UtcNow;
            bookmark.isActive = true;

            await _unitOfWork.BookmarkRepository.AddAsync(bookmark);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CreateBookmarksCommandResponse(Succeeded: true, Message: "Yer işareti başarıyla eklenmiştir.");
        }
    }
}
