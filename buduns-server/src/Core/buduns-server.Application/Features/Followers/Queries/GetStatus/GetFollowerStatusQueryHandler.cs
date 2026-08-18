using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Followers.Queries.GetStatus
{
    public class GetFollowerStatusQueryHandler : IRequestHandler<GetFollowerStatusQuery, GetFollowerStatusQueryResponse>
    {
        private readonly IFollowerRepository _followerRepository;
        private readonly IUserRepository _userRepository;

        public GetFollowerStatusQueryHandler(IFollowerRepository followerRepository, IUserRepository userRepository)
        {
            _followerRepository = followerRepository;
            _userRepository = userRepository;
        }

        public async Task<GetFollowerStatusQueryResponse> Handle(GetFollowerStatusQuery request, CancellationToken cancellationToken)
        {
            var followingUser = await _userRepository.GetByIdAsync(request.FollowingId, cancellationToken);
            if (followingUser == null || followingUser.Status == UserStatus.Banned)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            var follow = await _followerRepository.GetByUsersAsync(request.UserId, request.FollowingId, cancellationToken);
            return new GetFollowerStatusQueryResponse(IsFollowing: follow != null, FollowId: follow?.Id);
        }
    }
}
