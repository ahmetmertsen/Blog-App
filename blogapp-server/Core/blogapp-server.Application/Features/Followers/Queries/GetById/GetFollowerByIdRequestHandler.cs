using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Followers.Queries.GetById
{
    public class GetFollowerByIdRequestHandler : IRequestHandler<GetFollowerByIdRequest, FollowerDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetFollowerByIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<FollowerDto> Handle(GetFollowerByIdRequest request, CancellationToken cancellationToken)
        {
            var follower = await _unitOfWork.FollowerRepository.GetByIdAsync(request.Id);
            if (follower == null)
            {
                throw new NotFoundException("Takipçi bulunamadı!");
            }
            var response = _mapper.Map<FollowerDto>(follower);
            return response;
        }
    }
}
