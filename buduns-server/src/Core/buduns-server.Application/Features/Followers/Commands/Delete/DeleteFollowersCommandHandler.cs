using buduns_server.Application.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace buduns_server.Application.Features.Followers.Commands.Delete
{
    public class DeleteFollowersCommandHandler : IRequestHandler<DeleteFollowersCommand, DeleteFollowersCommandResponse>
    {
        private readonly IFollowerRepository _followerRepository;
        private readonly ILogger<DeleteFollowersCommandHandler> _logger;

        public DeleteFollowersCommandHandler(IFollowerRepository followerRepository, ILogger<DeleteFollowersCommandHandler> logger)
        {
            _followerRepository = followerRepository;
            _logger = logger;
        }

        public async Task<DeleteFollowersCommandResponse> Handle(DeleteFollowersCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _followerRepository.DeleteByUsersAsync(request.UserId, request.FollowingId, cancellationToken);
            if (deleted)
            {
                _logger.LogInformation("User unfollowed. FollowerUserId: {FollowerUserId}, FollowingUserId: {FollowingUserId}", request.UserId, request.FollowingId);
            }

            var message = deleted ? "Takip bırakıldı." : "Kullanıcı zaten takip edilmiyor.";
            return new DeleteFollowersCommandResponse(Message: message);
        }
    }
}
