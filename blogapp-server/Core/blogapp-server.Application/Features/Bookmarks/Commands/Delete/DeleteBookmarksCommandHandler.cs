using AutoMapper;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Bookmarks.Commands.Delete
{
    public class DeleteBookmarksCommandHandler : IRequestHandler<DeleteBookmarksCommand, DeleteBookmarksCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public DeleteBookmarksCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<DeleteBookmarksCommandResponse> Handle(DeleteBookmarksCommand request, CancellationToken cancellationToken)
        {
            var bookmark = await _unitOfWork.BookmarkRepository.GetByIdAsync(request.Id);
            if (bookmark == null)
            {
                throw new NotFoundException("Yer işareti bulunamadı!");
            }
            if (bookmark.UserId != request.UserId)
            {
                throw new UnauthorizedAccesException("Bu yer işaretini silmeye yetkiniz yoktur!");
            }

            await _unitOfWork.BookmarkRepository.DeleteAsync(bookmark.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DeleteBookmarksCommandResponse(Succeeded: true, Message: "Yer işareti başarıyla silinmiştir");
        }
    }
}
