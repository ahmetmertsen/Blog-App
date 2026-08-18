using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Comments.Queries.GetByPostId
{
    public class GetCommentsByPostIdQueryHandler : IRequestHandler<GetCommentsByPostIdQuery, PagedResponse<CommentDto>>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IPostRepository _postRepository;

        public GetCommentsByPostIdQueryHandler(ICommentRepository commentRepository, IPostRepository postRepository)
        {
            _commentRepository = commentRepository;
            _postRepository = postRepository;
        }

        public async Task<PagedResponse<CommentDto>> Handle(GetCommentsByPostIdQuery request, CancellationToken cancellationToken)
        {
            if (!await _postRepository.ExistsVisibleAsync(request.PostId, cancellationToken))
            {
                throw new NotFoundException("Paylaşım bulunamadı.");
            }

            var result = await _commentRepository.GetPagedByPostIdAsync(request.PostId, request.Page, request.Size, cancellationToken);
            return new PagedResponse<CommentDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
