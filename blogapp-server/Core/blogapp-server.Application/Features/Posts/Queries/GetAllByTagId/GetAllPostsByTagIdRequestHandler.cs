using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Queries.GetAllByTagId
{
    public class GetAllPostsByTagIdRequestHandler : IRequestHandler<GetAllPostsByTagIdRequest, List<PostDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllPostsByTagIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<PostDto>> Handle(GetAllPostsByTagIdRequest request, CancellationToken cancellationToken)
        {
            var posts = await _unitOfWork.PostRepository.GetAllByTagIdAsync(request.TagId);
            var response = _mapper.Map<List<PostDto>>(posts);
            return response;
        }
    }
}
