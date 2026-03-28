using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Followers.Queries.GetAllByUserId
{
    public class GetAllFollowingsByUserIdRequestHandler : IRequestHandler<GetAllFollowingsByUserIdRequest, List<FollowerDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllFollowingsByUserIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<FollowerDto>> Handle(GetAllFollowingsByUserIdRequest request, CancellationToken cancellationToken)
        {
            var followings = await _unitOfWork.FollowerRepository.GetAllFollowingsByUserIdAsync(request.UserId);
            var response = _mapper.Map<List<FollowerDto>>(followings);
            return response;
        }
    }
}
