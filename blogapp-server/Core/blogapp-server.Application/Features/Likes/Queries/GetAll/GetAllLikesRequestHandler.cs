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

namespace blogapp_server.Application.Features.Likes.Queries.GetAll
{
    public class GetAllLikesRequestHandler : IRequestHandler<GetAllLikesRequest, List<LikeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllLikesRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<LikeDto>> Handle(GetAllLikesRequest request, CancellationToken cancellationToken)
        {
            var likes = await _unitOfWork.LikeRepository.GetAllAsync();
            var response = _mapper.Map<List<LikeDto>>(likes);
            return response;
        }
    }
}
