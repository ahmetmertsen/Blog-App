using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Queries.GetByUserId
{
    public class GetCommentsByUserIdRequestHandler : IRequestHandler<GetCommentsByUserIdRequest, List<CommentDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetCommentsByUserIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<CommentDto>> Handle(GetCommentsByUserIdRequest request, CancellationToken cancellationToken)
        {
            var comments = await _unitOfWork.CommentRepository.GetCommentsByUserIdAsync(request.UserId);

            var response = _mapper.Map<List<CommentDto>>(comments);
            return response;
        }
    }
}
