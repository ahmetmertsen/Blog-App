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

namespace blogapp_server.Application.Features.Likes.Queries.GetById
{
    public class GetLikeByIdRequestHandler : IRequestHandler<GetLikeByIdRequest, LikeDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLikeByIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<LikeDto> Handle(GetLikeByIdRequest request, CancellationToken cancellationToken)
        {
            var like = await _unitOfWork.LikeRepository.GetByIdAsync(request.Id);
            if (like == null)
            {
                throw new NotFoundException("Like bulunamadı!");
            }
            var response = _mapper.Map<LikeDto>(like);
            return response;
        }
    }
}
