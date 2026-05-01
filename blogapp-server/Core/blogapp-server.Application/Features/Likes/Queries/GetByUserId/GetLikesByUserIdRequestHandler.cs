using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Likes.Queries.GetByUserId
{
    public class GetLikesByUserIdRequestHandler : IRequestHandler<GetLikesByUserIdRequest, List<LikeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLikesByUserIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<LikeDto>> Handle(GetLikesByUserIdRequest request, CancellationToken cancellationToken)
        {
            var likes = await _unitOfWork.LikeRepository.GetLikesByUserIdAsync(request.UserId);

            var response = _mapper.Map<List<LikeDto>>(likes);
            return response;
        }
    }
}
