using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace buduns_server.Application.Features.Followers.Commands.Create
{
    public class CreateFollowersCommandHandler : IRequestHandler<CreateFollowersCommand, CreateFollowersCommandResponse>
    {
        private readonly IFollowerRepository _followerRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateFollowersCommandHandler> _logger;
        private readonly INotificationService _notificationService;

        public CreateFollowersCommandHandler(IFollowerRepository followerRepository, INotificationRepository notificationRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger<CreateFollowersCommandHandler> logger, INotificationService notificationService)
        {
            _followerRepository = followerRepository;
            _notificationRepository = notificationRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<CreateFollowersCommandResponse> Handle(CreateFollowersCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == request.FollowingId)
            {
                throw new BadRequestException("Kullanıcı kendisini takip edemez!");
            }

            var followingUser = await _userRepository.GetByIdAsync(request.FollowingId, cancellationToken);
            if (followingUser == null)
            {
                throw new NotFoundException("Takip edilecek kullanıcı bulunamadı.");
            }

            if (followingUser.Status == UserStatus.Banned)
            {
                throw new BadRequestException("Bu kullanıcı takip edilemez.");
            }

            var follow = new Follower
            {
                FollowerId = request.UserId,
                FollowingId = request.FollowingId,
                CreatedAt = DateTime.UtcNow,
                isActive = true,
                isDeleted = false
            };

            var result = await _followerRepository.CreateIfNotExistsAsync(follow, cancellationToken);
            if (result.Created)
            {
                // Bildirim ayni transaction sinirinda yaziliyor; takip
                // kaydedilmezse bildirim de kalmaz.
                var notification = await _notificationService.BuildAsync(new NotificationCreateModel { Type = NotificationType.NEW_FOLLOWER, UserId = request.FollowingId, ActorUserId = request.UserId, Cooldown = TimeSpan.FromHours(24) }, cancellationToken);
                if (notification != null)
                {
                    await _notificationRepository.AddAsync(notification);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                _logger.LogInformation("User followed. FollowerUserId: {FollowerUserId}, FollowingUserId: {FollowingUserId}, FollowId: {FollowId}", request.UserId, request.FollowingId, result.Follower.Id);
            }

            var message = result.Created ? "Kullanıcı takip edildi." : "Bu kullanıcı zaten takip ediliyor.";
            return new CreateFollowersCommandResponse(Message: message, FollowId: result.Follower.Id, AlreadyFollowing: !result.Created);
        }
    }
}
