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

namespace buduns_server.Application.Features.Likes.Queries.GetById
{
    public class GetLikeByIdQueryHandler : IRequestHandler<GetLikeByIdQuery, LikeDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetLikeByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<LikeDto> Handle(GetLikeByIdQuery request, CancellationToken cancellationToken)
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
