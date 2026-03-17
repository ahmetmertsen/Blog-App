using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Likes.Queries.GetAllByUsername
{
    public class GetAllLikesByUsernameRequestHandler : IRequestHandler<GetAllLikesByUsernameRequest, List<LikeDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllLikesByUsernameRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<LikeDto>> Handle(GetAllLikesByUsernameRequest request, CancellationToken cancellationToken)
        {
            var likes = await _unitOfWork.LikeRepository.GetAllLikesByUsernameAsync(request.UserName);
            var response = _mapper.Map<List<LikeDto>>(likes);
            return response;
        }
    }
}
