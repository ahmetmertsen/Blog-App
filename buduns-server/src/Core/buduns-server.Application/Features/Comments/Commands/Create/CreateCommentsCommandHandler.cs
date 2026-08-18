using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Comments.Commands.Create
{
    public class CreateCommentsCommandHandler : IRequestHandler<CreateCommentsCommand, CreateCommentsCommandResponse>
    {
        private const int CommentLimitPerMinute = 10;
        private readonly ICommentRepository _commentRepository;
        private readonly IPostRepository _postRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;

        public CreateCommentsCommandHandler(ICommentRepository commentRepository, IPostRepository postRepository, IUnitOfWork unitOfWork, INotificationService notificationService)
        {
            _commentRepository = commentRepository;
            _postRepository = postRepository;
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
        }

        public async Task<CreateCommentsCommandResponse> Handle(CreateCommentsCommand request, CancellationToken cancellationToken)
        {
            var postOwnerId = await _postRepository.GetVisibleOwnerIdAsync(request.PostId, cancellationToken);
            if (!postOwnerId.HasValue)
            {
                throw new NotFoundException("Yorum yapılacak paylaşım bulunamadı.");
            }

            var content = request.Content.Trim();
            var now = DateTime.UtcNow;
            var recentCommentCount = await _commentRepository.CountRecentByUserAsync(request.UserId, now.AddMinutes(-1), cancellationToken);
            if (recentCommentCount >= CommentLimitPerMinute)
            {
                throw new TooManyRequestsException("Bir dakika içinde en fazla 10 yorum oluşturabilirsiniz.");
            }

            var isDuplicate = await _commentRepository.HasRecentDuplicateAsync(request.UserId, request.PostId, content, now.AddSeconds(-60), cancellationToken);
            if (isDuplicate)
            {
                throw new BadRequestException("Aynı yorumu kısa süre içinde tekrar gönderemezsiniz.");
            }

            var comment = new Comment
            {
                PostId = request.PostId,
                UserId = request.UserId,
                Content = content,
                Status = CommentStatus.Published,
                CreatedAt = now,
                isActive = true,
                isDeleted = false
            };
            await _commentRepository.AddAsync(comment);

            await _notificationService.AddAsync(new NotificationCreateModel { Type = NotificationType.POST_COMMENTED, UserId = postOwnerId.Value, ActorUserId = request.UserId, PostId = request.PostId, Comment = comment }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var commentDto = await _commentRepository.GetDtoByIdAsync(comment.Id, cancellationToken) ?? throw new NotFoundException("Oluşturulan yorum bulunamadı.");
            return new CreateCommentsCommandResponse("Yorum başarıyla eklendi.", commentDto);
        }
    }
}
