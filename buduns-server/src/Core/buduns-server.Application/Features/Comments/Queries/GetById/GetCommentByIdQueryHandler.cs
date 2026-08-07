using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Comments.Queries.GetById
{
    public class GetCommentByIdQueryHandler : IRequestHandler<GetCommentByIdQuery, CommentDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetCommentByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CommentDto> Handle(GetCommentByIdQuery request, CancellationToken cancellationToken)
        {
            return await _unitOfWork.CommentRepository.GetDtoByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Yorum bulunamadı.");
        }
    }
}
