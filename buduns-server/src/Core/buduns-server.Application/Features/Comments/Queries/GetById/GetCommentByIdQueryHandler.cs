using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Comments.Queries.GetById
{
    public class GetCommentByIdQueryHandler : IRequestHandler<GetCommentByIdQuery, CommentDto>
    {
        private readonly ICommentRepository _commentRepository;

        public GetCommentByIdQueryHandler(ICommentRepository commentRepository)
        {
            _commentRepository = commentRepository;
        }

        public async Task<CommentDto> Handle(GetCommentByIdQuery request, CancellationToken cancellationToken)
        {
            return await _commentRepository.GetDtoByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Yorum bulunamadı.");
        }
    }
}
