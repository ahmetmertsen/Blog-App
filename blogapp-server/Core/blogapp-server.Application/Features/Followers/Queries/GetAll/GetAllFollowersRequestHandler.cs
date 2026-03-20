using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Followers.Queries.GetAll
{
    public class GetAllFollowersRequestHandler : IRequestHandler<GetAllFollowersRequest, List<FollowerDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllFollowersRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<FollowerDto>> Handle(GetAllFollowersRequest request, CancellationToken cancellationToken)
        {
            var followers = await _unitOfWork.FollowerRepository.GetAllAsync();
            var response = _mapper.Map<List<FollowerDto>>(followers);
            return response;
        }
    }
}
