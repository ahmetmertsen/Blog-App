using buduns_server.Application.Exceptions;
using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Likes.Commands.Create
{
    public class CreateLikesCommandHandler : IRequestHandler<CreateLikesCommand, CreateLikesCommandResponse>
    {
        private readonly ILikeRepository _likeRepository;
        private readonly IPostRepository _postRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public CreateLikesCommandHandler(ILikeRepository likeRepository, IPostRepository postRepository, INotificationRepository notificationRepository, IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _likeRepository = likeRepository;
            _postRepository = postRepository;
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<CreateLikesCommandResponse> Handle(CreateLikesCommand request, CancellationToken cancellationToken)
        {
            var postOwnerId = await _postRepository.GetVisibleOwnerIdAsync(request.PostId, cancellationToken);
            if (!postOwnerId.HasValue)
            {
                throw new NotFoundException("Beğenilecek paylaşım bulunamadı.");
            }

            var now = DateTime.UtcNow;
            var like = new Like { UserId = request.UserId, PostId = request.PostId, CreatedAt = now, isActive = true, isDeleted = false };
            var result = await _likeRepository.CreateIfNotExistsAsync(like, cancellationToken);

            // Bildirim yalnizca gercekten begeni olustuysa yaziliyor ve ayni
            // transaction sinirinda kalici oluyor.
            if (result.Created)
            {
                var notification = await _notificationService.BuildAsync(new NotificationCreateModel { Type = NotificationType.POST_LIKED, UserId = postOwnerId.Value, ActorUserId = request.UserId, PostId = request.PostId, Cooldown = TimeSpan.FromHours(1) }, cancellationToken);
                if (notification != null)
                {
                    await _notificationRepository.AddAsync(notification);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
            }

            var message = result.Created ? "Paylaşım beğenildi." : "Paylaşım zaten beğenilmiş.";
            return new CreateLikesCommandResponse(Message: message, LikeId: result.Like.Id, AlreadyLiked: !result.Created);
        }
    }
}
