using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.Features.Likes.Queries.GetById;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Bookmarks.Queries.GetById
{
    public class GetBookmarkByIdRequestHandler : IRequestHandler<GetBookmarkByIdRequest, BookmarkDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetBookmarkByIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<BookmarkDto> Handle(GetBookmarkByIdRequest request, CancellationToken cancellationToken)
        {
            var bookmark = await _unitOfWork.BookmarkRepository.GetByIdAsync(request.Id);
            if (bookmark == null)
            {
                throw new NotFoundException("Yer işareti bulunamadı!");
            }
            var response = _mapper.Map<BookmarkDto>(bookmark);
            return response;
        }
    }
}
