using AutoMapper;
using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Followers.Queries.GetById
{
    public class GetFollowerByIdQueryHandler : IRequestHandler<GetFollowerByIdQuery, FollowerDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetFollowerByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<FollowerDto> Handle(GetFollowerByIdQuery request, CancellationToken cancellationToken)
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
