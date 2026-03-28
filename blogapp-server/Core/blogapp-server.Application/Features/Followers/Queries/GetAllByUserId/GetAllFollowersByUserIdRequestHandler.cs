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
    public class GetAllFollowersByUserIdRequestHandler : IRequestHandler<GetAllFollowersByUserIdRequest, List<FollowerDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllFollowersByUserIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<FollowerDto>> Handle(GetAllFollowersByUserIdRequest request, CancellationToken cancellationToken)
        {
            var followers = await _unitOfWork.FollowerRepository.GetAllFollowersByUserIdAsync(request.UserId);
            var response = _mapper.Map<List<FollowerDto>>(followers);
            return response;
        }
    }
}
