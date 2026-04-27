using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Bookmarks.Queries.GetUserId
{
    public class GetBookmarksByUserIdRequestHandler : IRequestHandler<GetBookmarksByUserIdRequest, List<BookmarkDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetBookmarksByUserIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<BookmarkDto>> Handle(GetBookmarksByUserIdRequest request, CancellationToken cancellationToken)
        {
            var bookmarks = await _unitOfWork.BookmarkRepository.GetBookmarksByUserIdAsync(request.UserId);

            var response = _mapper.Map<List<BookmarkDto>>(bookmarks);
            return response;
        }
    }
}
