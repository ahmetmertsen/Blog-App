using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Bookmarks.Queries.GetAll
{
    public class GetAllBookmarksRequestHandler : IRequestHandler<GetAllBookmarksRequest, List<BookmarkDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllBookmarksRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<BookmarkDto>> Handle(GetAllBookmarksRequest request, CancellationToken cancellationToken)
        {
            var bookmarks = await _unitOfWork.BookmarkRepository.GetAllAsync();
            var response = _mapper.Map<List<BookmarkDto>>(bookmarks);
            return response;
        }
    }
}
