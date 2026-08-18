using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Likes.Queries.GetMyLikes
{
    public class GetMyLikedPostsQueryHandler : IRequestHandler<GetMyLikedPostsQuery, PagedResponse<LikedPostDto>>
    {
        private readonly ILikeRepository _likeRepository;

        public GetMyLikedPostsQueryHandler(ILikeRepository likeRepository)
        {
            _likeRepository = likeRepository;
        }

        public async Task<PagedResponse<LikedPostDto>> Handle(GetMyLikedPostsQuery request, CancellationToken cancellationToken)
        {
            var result = await _likeRepository.GetPagedByUserIdAsync(request.UserId, request.Page, request.Size, cancellationToken);
            return new PagedResponse<LikedPostDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
