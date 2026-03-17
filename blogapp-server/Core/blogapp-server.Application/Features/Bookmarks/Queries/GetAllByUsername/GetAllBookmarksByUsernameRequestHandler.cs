using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Features.Likes.Queries.GetAllByUsername;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Bookmarks.Queries.GetAllByUsername
{
    public class GetAllBookmarksByUsernameRequestHandler : IRequestHandler<GetAllBookmarksByUsernameRequest, List<BookmarkDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllBookmarksByUsernameRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<BookmarkDto>> Handle(GetAllBookmarksByUsernameRequest request, CancellationToken cancellationToken)
        {
            var bookmarks = await _unitOfWork.BookmarkRepository.GetAllBookmarksByUsernameAsync(request.UserName);
            var response = _mapper.Map<List<BookmarkDto>>(bookmarks);
            return response;
        }
    }
}
