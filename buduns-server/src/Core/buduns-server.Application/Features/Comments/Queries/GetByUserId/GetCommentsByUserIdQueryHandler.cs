using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Comments.Queries.GetByUserId
{
    public class GetCommentsByUserIdQueryHandler : IRequestHandler<GetCommentsByUserIdQuery, PagedResponse<CommentDto>>
    {
        private readonly ICommentRepository _commentRepository;

        public GetCommentsByUserIdQueryHandler(ICommentRepository commentRepository)
        {
            _commentRepository = commentRepository;
        }

        public async Task<PagedResponse<CommentDto>> Handle(GetCommentsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _commentRepository.GetPagedByUserIdAsync(request.UserId, request.Page, request.Size, cancellationToken);
            return new PagedResponse<CommentDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
