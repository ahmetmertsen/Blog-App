using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Likes.Queries.GetByPostId
{
    public class GetLikesByPostIdQueryHandler : IRequestHandler<GetLikesByPostIdQuery, PagedResponse<LikeDto>>
    {
        private readonly ILikeRepository _likeRepository;
        private readonly IPostRepository _postRepository;

        public GetLikesByPostIdQueryHandler(ILikeRepository likeRepository, IPostRepository postRepository)
        {
            _likeRepository = likeRepository;
            _postRepository = postRepository;
        }

        public async Task<PagedResponse<LikeDto>> Handle(GetLikesByPostIdQuery request, CancellationToken cancellationToken)
        {
            if (!await _postRepository.ExistsVisibleAsync(request.PostId, cancellationToken))
            {
                throw new NotFoundException("Paylaşım bulunamadı.");
            }

            var result = await _likeRepository.GetPagedByPostIdAsync(request.PostId, request.Page, request.Size, cancellationToken);
            return new PagedResponse<LikeDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
