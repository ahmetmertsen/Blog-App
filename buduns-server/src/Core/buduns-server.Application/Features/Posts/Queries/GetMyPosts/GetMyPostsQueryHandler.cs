using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Posts.Queries.GetMyPosts
{
    public class GetMyPostsQueryHandler : IRequestHandler<GetMyPostsQuery, PagedResponse<PostDto>>
    {
        private readonly IPostRepository _postRepository;

        public GetMyPostsQueryHandler(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<PagedResponse<PostDto>> Handle(GetMyPostsQuery request, CancellationToken cancellationToken)
        {
            var result = await _postRepository.GetPagedByUserIdAsync(request.UserId, request.Page, request.Size, request.UserId, cancellationToken);
            return new PagedResponse<PostDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
