using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using ReportEntity = buduns_server.Domain.Entities.Report;

namespace buduns_server.Application.Features.Report.Commands.ReviewReport
{
    public class ReviewReportCommandHandler : IRequestHandler<ReviewReportCommand, ReviewReportCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ReviewReportCommandHandler> _logger;
        private readonly IAuthSessionService _authSessionService;
        private readonly INotificationService _notificationService;

        public ReviewReportCommandHandler(IUnitOfWork unitOfWork, ILogger<ReviewReportCommandHandler> logger, IAuthSessionService authSessionService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _authSessionService = authSessionService;
            _notificationService = notificationService;
        }

        public async Task<ReviewReportCommandResponse> Handle(ReviewReportCommand request, CancellationToken cancellationToken)
        {
            var report = await _unitOfWork.ReportRepository.GetByIdWithDetailsAsync(request.ReportId);
            if (report == null)
            {
                throw new NotFoundException("Şikayet bulunamadı.");
            }

            if (report.Status != ReportStatus.Pending && report.Status != ReportStatus.InReview)
            {
                throw new BadRequestException("Bu şikayet zaten sonuçlandırılmış.");
            }

            var targetId = GetTargetId(report);
            var openReports = await _unitOfWork.ReportRepository.GetOpenReportsForTargetAsync(report.TargetType, targetId, cancellationToken);
            EnsureReportsCanBeHandledByModerator(openReports, request.UserId);

            if (request.Status == ReportStatus.InReview)
            {
                var inReviewAt = DateTime.UtcNow;
                foreach (var openReport in openReports)
                {
                    openReport.Status = ReportStatus.InReview;
                    openReport.ReviewNote = request.ReviewNote?.Trim();
                    openReport.ReviewedByUserId = request.UserId;
                    openReport.UpdateAt = inReviewAt;
                    _unitOfWork.ReportRepository.Update(openReport);
                }

                await SaveChangesWithConcurrencyCheckAsync(cancellationToken);
                _logger.LogInformation("Report review started. ReportId: {ReportId}, ModeratorUserId: {ModeratorUserId}, TargetType: {TargetType}, TargetId: {TargetId}, ClaimedReportCount: {ClaimedReportCount}", report.Id, request.UserId, report.TargetType, targetId, openReports.Count);
                return new ReviewReportCommandResponse("Şikayet ve aynı hedefe ait açık şikayetler incelemeye alındı.");
            }

            ValidateActionForTarget(report, request.ActionType);
            var actionTargetUserId = ResolveActionTargetUserId(report, request.ActionType);
            if (actionTargetUserId == request.UserId && IsUserAction(request.ActionType))
            {
                throw new BadRequestException("Moderatör kendi hesabına moderasyon aksiyonu uygulayamaz.");
            }

            var actionExpiresAt = await ApplyModerationActionAsync(report, request, cancellationToken);
            var reviewedAt = DateTime.UtcNow;

            foreach (var openReport in openReports)
            {
                openReport.Status = request.Status;
                openReport.ReviewNote = request.ReviewNote?.Trim();
                openReport.ReviewedByUserId = request.UserId;
                openReport.ReviewedDate = reviewedAt;
                openReport.UpdateAt = reviewedAt;
                _unitOfWork.ReportRepository.Update(openReport);
            }

            var moderationAction = new ModerationAction
            {
                ReportId = report.Id,
                ModeratorUserId = request.UserId,
                ActionType = request.ActionType,
                TargetType = report.TargetType,
                TargetPostId = report.TargetPostId,
                TargetUserId = ResolveActionTargetUserId(report, request.ActionType),
                TargetCommentId = report.TargetCommentId,
                Note = request.ReviewNote?.Trim(),
                ExpiresAt = actionExpiresAt,
                CreatedAt = reviewedAt,
                UpdateAt = reviewedAt,
                isActive = true,
                isDeleted = false
            };
            await _unitOfWork.ModerationActionRepository.AddAsync(moderationAction);
            await AddReporterNotificationsAsync(openReports, cancellationToken);
            await SaveChangesWithConcurrencyCheckAsync(cancellationToken);

            if (actionTargetUserId.HasValue && (request.ActionType == ModerationActionType.SuspendUser || request.ActionType == ModerationActionType.BanUser))
            {
                await _authSessionService.RevokeAllSessionsAsync(actionTargetUserId.Value, request.ActionType == ModerationActionType.BanUser ? "Account banned" : "Account suspended", cancellationToken);
            }

            _logger.LogInformation("Report resolved. ReportId: {ReportId}, ModeratorUserId: {ModeratorUserId}, Status: {Status}, ActionType: {ActionType}, TargetType: {TargetType}, TargetId: {TargetId}, ResolvedReportCount: {ResolvedReportCount}", report.Id, request.UserId, request.Status, request.ActionType, report.TargetType, targetId, openReports.Count);
            return new ReviewReportCommandResponse("Şikayet ve aynı hedefe ait açık şikayetler sonuçlandırıldı.");
        }

        private async Task<DateTime?> ApplyModerationActionAsync(ReportEntity report, ReviewReportCommand request, CancellationToken cancellationToken)
        {
            switch (request.ActionType)
            {
                case ModerationActionType.None:
                    return null;
                case ModerationActionType.HidePost:
                    var postToHide = GetTargetPost(report);
                    postToHide.Status = PostStatus.HiddenByModerator;
                    postToHide.isPublished = false;
                    postToHide.isActive = false;
                    postToHide.UpdateAt = DateTime.UtcNow;
                    _unitOfWork.PostRepository.Update(postToHide);
                    await AddTargetNotificationAsync(postToHide.UserId, NotificationType.POST_HIDDEN, "Paylaşımınız moderasyon kararıyla gizlendi.", postToHide.Id, null, cancellationToken);
                    return null;
                case ModerationActionType.DeletePost:
                    var postToDelete = GetTargetPost(report);
                    postToDelete.Status = PostStatus.DeletedByModerator;
                    postToDelete.isPublished = false;
                    postToDelete.isActive = false;
                    postToDelete.isDeleted = true;
                    postToDelete.UpdateAt = DateTime.UtcNow;
                    _unitOfWork.PostRepository.Update(postToDelete);
                    await AddTargetNotificationAsync(postToDelete.UserId, NotificationType.POST_REMOVED, "Paylaşımınız moderasyon kararıyla kaldırıldı.", postToDelete.Id, null, cancellationToken);
                    return null;
                case ModerationActionType.HideComment:
                    var commentToHide = GetTargetComment(report);
                    EnsureCommentCanBeModerated(commentToHide);
                    commentToHide.Status = CommentStatus.HiddenByModerator;
                    commentToHide.isActive = false;
                    commentToHide.UpdateAt = DateTime.UtcNow;
                    await AddTargetNotificationAsync(commentToHide.UserId, NotificationType.COMMENT_HIDDEN, "Yorumunuz moderasyon kararıyla gizlendi.", commentToHide.PostId, commentToHide.Id, cancellationToken);
                    return null;
                case ModerationActionType.DeleteComment:
                    var commentToDelete = GetTargetComment(report);
                    EnsureCommentCanBeModerated(commentToDelete);
                    commentToDelete.Status = CommentStatus.DeletedByModerator;
                    commentToDelete.isActive = false;
                    commentToDelete.isDeleted = true;
                    commentToDelete.UpdateAt = DateTime.UtcNow;
                    await AddTargetNotificationAsync(commentToDelete.UserId, NotificationType.COMMENT_REMOVED, "Yorumunuz moderasyon kararıyla kaldırıldı.", commentToDelete.PostId, commentToDelete.Id, cancellationToken);
                    return null;
                case ModerationActionType.WarnUser:
                    var warnedUserId = ResolveActionTargetUserId(report, request.ActionType) ?? throw new BadRequestException("Uyarılacak kullanıcı bulunamadı.");
                    await AddTargetNotificationAsync(warnedUserId, NotificationType.MODERATION_WARNING, "Hesabınız bir topluluk kuralı ihlali nedeniyle uyarıldı.", null, null, cancellationToken);
                    return null;
                case ModerationActionType.SuspendUser:
                    var suspendedUser = await GetActionTargetUserAsync(report, cancellationToken);
                    var suspensionEnd = DateTime.UtcNow.AddDays(request.SuspensionDays!.Value);
                    suspendedUser.Status = UserStatus.Suspended;
                    suspendedUser.SuspendedUntil = suspensionEnd;
                    await AddTargetNotificationAsync(suspendedUser.Id, NotificationType.ACCOUNT_SUSPENDED, $"Hesabınız {request.SuspensionDays.Value} gün süreyle askıya alındı.", null, null, cancellationToken);
                    return suspensionEnd;
                case ModerationActionType.BanUser:
                    var bannedUser = await GetActionTargetUserAsync(report, cancellationToken);
                    bannedUser.Status = UserStatus.Banned;
                    bannedUser.SuspendedUntil = null;
                    await AddTargetNotificationAsync(bannedUser.Id, NotificationType.ACCOUNT_BANNED, "Hesabınız moderasyon kararıyla kalıcı olarak yasaklandı.", null, null, cancellationToken);
                    return null;
                default:
                    throw new BadRequestException("Desteklenmeyen moderasyon aksiyonu.");
            }
        }

        private static void EnsureReportsCanBeHandledByModerator(IEnumerable<ReportEntity> reports, int moderatorUserId)
        {
            if (reports.Any(report => report.Status == ReportStatus.InReview && report.ReviewedByUserId.HasValue && report.ReviewedByUserId.Value != moderatorUserId))
            {
                throw new BadRequestException("Bu hedefe ait açık şikayetlerden biri başka bir moderatör tarafından inceleniyor.");
            }
        }

        private static void ValidateActionForTarget(ReportEntity report, ModerationActionType actionType)
        {
            if ((actionType == ModerationActionType.HidePost || actionType == ModerationActionType.DeletePost) && report.TargetType != ReportTargetType.Post)
            {
                throw new BadRequestException("Post moderasyon aksiyonu yalnızca post şikayetlerine uygulanabilir.");
            }

            if ((actionType == ModerationActionType.HideComment || actionType == ModerationActionType.DeleteComment) && report.TargetType != ReportTargetType.Comment)
            {
                throw new BadRequestException("Yorum moderasyon aksiyonu yalnızca yorum şikayetlerine uygulanabilir.");
            }
        }

        private static bool IsUserAction(ModerationActionType actionType) => actionType == ModerationActionType.WarnUser || actionType == ModerationActionType.SuspendUser || actionType == ModerationActionType.BanUser;

        private static Post GetTargetPost(ReportEntity report) => report.TargetPost ?? throw new NotFoundException("Şikayet edilen paylaşım bulunamadı.");

        private static Comment GetTargetComment(ReportEntity report) => report.TargetComment ?? throw new NotFoundException("Şikayet edilen yorum bulunamadı.");

        private static void EnsureCommentCanBeModerated(Comment comment)
        {
            if (comment.Status != CommentStatus.Published || !comment.isActive || comment.isDeleted)
            {
                throw new BadRequestException("Yorum artık moderasyon aksiyonu uygulanabilecek durumda değil.");
            }
        }

        private async Task<User> GetActionTargetUserAsync(ReportEntity report, CancellationToken cancellationToken)
        {
            var targetUserId = ResolveActionTargetUserId(report, ModerationActionType.WarnUser) ?? throw new NotFoundException("Moderasyon uygulanacak kullanıcı bulunamadı.");
            return await _unitOfWork.UserRepository.GetByIdAsync(targetUserId, cancellationToken) ?? throw new NotFoundException("Moderasyon uygulanacak kullanıcı bulunamadı.");
        }

        private static int? ResolveActionTargetUserId(ReportEntity report, ModerationActionType actionType)
        {
            if (!IsUserAction(actionType))
            {
                return report.TargetUserId;
            }

            return report.TargetType switch
            {
                ReportTargetType.User => report.TargetUserId,
                ReportTargetType.Post => report.TargetPost?.UserId,
                ReportTargetType.Comment => report.TargetComment?.UserId,
                _ => null
            };
        }

        private static int GetTargetId(ReportEntity report) => report.TargetType switch
        {
            ReportTargetType.Post => report.TargetPostId ?? throw new BadRequestException("Şikayet hedefi bulunamadı."),
            ReportTargetType.User => report.TargetUserId ?? throw new BadRequestException("Şikayet hedefi bulunamadı."),
            ReportTargetType.Comment => report.TargetCommentId ?? throw new BadRequestException("Şikayet hedefi bulunamadı."),
            _ => throw new BadRequestException("Şikayet hedefi geçersiz.")
        };

        private async Task AddReporterNotificationsAsync(IEnumerable<ReportEntity> reports, CancellationToken cancellationToken)
        {
            foreach (var reporterUserId in reports.Select(report => report.ReporterUserId).Distinct())
            {
                await AddTargetNotificationAsync(reporterUserId, NotificationType.REPORT_RESOLVED, "Oluşturduğunuz bir şikayet moderasyon ekibi tarafından sonuçlandırıldı.", null, null, cancellationToken);
            }
        }

        private async Task AddTargetNotificationAsync(int userId, NotificationType type, string message, int? postId, int? commentId, CancellationToken cancellationToken)
        {
            await _notificationService.AddAsync(new NotificationCreateModel { UserId = userId, Type = type, Message = message, PostId = postId, CommentId = commentId }, cancellationToken);
        }

        private async Task SaveChangesWithConcurrencyCheckAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                throw new BadRequestException("Bu şikayet başka bir moderatör tarafından güncellendi. Güncel durumu yeniden yükleyin.");
            }
        }
    }
}
