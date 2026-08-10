using buduns_server.Application.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace buduns_server.Application.Features.Followers.Commands.Delete
{
    public class DeleteFollowersCommandHandler : IRequestHandler<DeleteFollowersCommand, DeleteFollowersCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteFollowersCommandHandler> _logger;

        public DeleteFollowersCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteFollowersCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DeleteFollowersCommandResponse> Handle(DeleteFollowersCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _unitOfWork.FollowerRepository.DeleteByUsersAsync(request.UserId, request.FollowingId, cancellationToken);
            if (deleted)
            {
                _logger.LogInformation("User unfollowed. FollowerUserId: {FollowerUserId}, FollowingUserId: {FollowingUserId}", request.UserId, request.FollowingId);
            }

            var message = deleted ? "Takip bırakıldı." : "Kullanıcı zaten takip edilmiyor.";
            return new DeleteFollowersCommandResponse(Message: message);
        }
    }
}
