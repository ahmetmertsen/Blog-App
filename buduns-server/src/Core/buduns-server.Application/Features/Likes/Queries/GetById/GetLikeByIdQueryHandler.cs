using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Mapping;
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

        public GetLikeByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<LikeDto> Handle(GetLikeByIdQuery request, CancellationToken cancellationToken)
        {
            var like = await _unitOfWork.LikeRepository.GetByIdAsync(request.Id);
            if (like == null)
            {
                throw new NotFoundException("Like bulunamadı!");
            }
            var response = like.ToDto();
            return response;
        }
    }
}
