using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Queries.GetAll
{
    public class GetAllPostsRequestHandler : IRequestHandler<GetAllPostsRequest,List<PostDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllPostsRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<PostDto>> Handle(GetAllPostsRequest request, CancellationToken cancellationToken)
        {
            var posts = await _unitOfWork.PostRepository.GetAllAsync();
            var response = _mapper.Map<List<PostDto>>(posts);
            return response;
        }
    }
}
