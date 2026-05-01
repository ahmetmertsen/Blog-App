using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Likes.Queries.GetByPostId
{
    public class GetLikesByPostIdRequestHandler : IRequestHandler<GetLikesByPostIdRequest, List<LikeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLikesByPostIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<LikeDto>> Handle(GetLikesByPostIdRequest request, CancellationToken cancellationToken)
        {
            var likes = await _unitOfWork.LikeRepository.GetLikesByPostIdAsync(request.PostId);

            var response =_mapper.Map<List<LikeDto>>(likes);
            return response;
        }
    }
}
