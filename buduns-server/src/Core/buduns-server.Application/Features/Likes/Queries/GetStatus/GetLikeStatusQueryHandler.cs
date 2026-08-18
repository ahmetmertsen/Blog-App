using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Likes.Queries.GetStatus
{
    public class GetLikeStatusQueryHandler : IRequestHandler<GetLikeStatusQuery, GetLikeStatusQueryResponse>
    {
        private readonly ILikeRepository _likeRepository;
        private readonly IPostRepository _postRepository;

        public GetLikeStatusQueryHandler(ILikeRepository likeRepository, IPostRepository postRepository)
        {
            _likeRepository = likeRepository;
            _postRepository = postRepository;
        }

        public async Task<GetLikeStatusQueryResponse> Handle(GetLikeStatusQuery request, CancellationToken cancellationToken)
        {
            if (!await _postRepository.ExistsVisibleAsync(request.PostId, cancellationToken))
            {
                throw new NotFoundException("Paylaşım bulunamadı.");
            }

            var like = await _likeRepository.GetByUserAndPostAsync(request.UserId, request.PostId, cancellationToken);
            return new GetLikeStatusQueryResponse(IsLiked: like != null, LikeId: like?.Id);
        }
    }
}
