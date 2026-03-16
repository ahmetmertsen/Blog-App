using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Queries.GetById
{
    public class GetPostByIdRequestHandler : IRequestHandler<GetPostByIdRequest, PostDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetPostByIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<PostDto> Handle(GetPostByIdRequest request, CancellationToken cancellationToken)
        {
            var post = await _unitOfWork.PostRepository.GetByIdAsync(request.Id);
            if (post == null)
            {
                //Exception yazılacak
            }

            var response = _mapper.Map<PostDto>(post);
            return response;
        }
    }
}
