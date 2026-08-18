using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Posts.Queries.GetFollowingPosts
{
    public class GetFollowingPostsQueryHandler : IRequestHandler<GetFollowingPostsQuery, PagedResponse<PostDto>>
    {
        private readonly IPostRepository _postRepository;

        public GetFollowingPostsQueryHandler(IPostRepository postRepository)
        {
            _postRepository = postRepository;
        }

        public async Task<PagedResponse<PostDto>> Handle(GetFollowingPostsQuery request, CancellationToken cancellationToken)
        {
            var result = await _postRepository.GetPagedFollowingAsync(request.UserId, request.Page, request.Size, cancellationToken);
            return new PagedResponse<PostDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
