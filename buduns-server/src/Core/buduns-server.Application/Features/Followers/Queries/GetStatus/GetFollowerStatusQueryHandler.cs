using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Followers.Queries.GetStatus
{
    public class GetFollowerStatusQueryHandler : IRequestHandler<GetFollowerStatusQuery, GetFollowerStatusQueryResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetFollowerStatusQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GetFollowerStatusQueryResponse> Handle(GetFollowerStatusQuery request, CancellationToken cancellationToken)
        {
            var followingUser = await _unitOfWork.UserRepository.GetByIdAsync(request.FollowingId, cancellationToken);
            if (followingUser == null || followingUser.Status == UserStatus.Banned)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            var follow = await _unitOfWork.FollowerRepository.GetByUsersAsync(request.UserId, request.FollowingId, cancellationToken);
            return new GetFollowerStatusQueryResponse(IsFollowing: follow != null, FollowId: follow?.Id);
        }
    }
}
