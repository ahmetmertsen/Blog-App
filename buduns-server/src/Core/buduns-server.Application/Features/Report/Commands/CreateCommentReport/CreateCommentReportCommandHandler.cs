using buduns_server.Application.Exceptions;
using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Common.Options;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace buduns_server.Application.Features.Report.Commands.CreateCommentReport
{
    public class CreateCommentReportCommandHandler : IRequestHandler<CreateCommentReportCommand, CreateCommentReportCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateCommentReportCommandHandler> _logger;
        private readonly ReportPolicyOptions _reportPolicyOptions;

        public CreateCommentReportCommandHandler(IUnitOfWork unitOfWork, ILogger<CreateCommentReportCommandHandler> logger, IOptions<ReportPolicyOptions> reportPolicyOptions)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _reportPolicyOptions = reportPolicyOptions.Value;
        }

        public async Task<CreateCommentReportCommandResponse> Handle(CreateCommentReportCommand request, CancellationToken cancellationToken)
        {
            var comment = await _unitOfWork.CommentRepository.GetVisibleByIdAsync(request.CommentId, cancellationToken);
            if (comment == null)
            {
                throw new NotFoundException("Şikayet edilecek yorum bulunamadı.");
            }

            if (comment.UserId == request.UserId)
            {
                throw new BadRequestException("Kendi yorumunuzu şikayet edemezsiniz.");
            }

            var recentReportCount = await _unitOfWork.ReportRepository.CountRecentReportsByUserAsync(request.UserId, DateTime.UtcNow.AddHours(-24), cancellationToken);
            var dailyReportLimit = Math.Max(1, _reportPolicyOptions.DailyReportLimit);
            if (recentReportCount >= dailyReportLimit)
            {
                throw new TooManyRequestsException($"24 saat içinde en fazla {dailyReportLimit} şikayet oluşturabilirsiniz.");
            }

            if (await _unitOfWork.ReportRepository.HasPendingCommentReportAsync(request.UserId, request.CommentId, cancellationToken))
            {
                throw new BadRequestException("Bu yorum için zaten bekleyen bir şikayetiniz var.");
            }

            var report = new Domain.Entities.Report
            {
                ReporterUserId = request.UserId,
                TargetType = ReportTargetType.Comment,
                TargetPostId = null,
                TargetUserId = null,
                TargetCommentId = request.CommentId,
                TargetOwnerUserId = comment.UserId,
                TargetOwnerUserNameSnapshot = comment.User?.UserName,
                TargetOwnerFullNameSnapshot = comment.User?.FullName,
                TargetContentSnapshot = ReportSnapshotHelper.CreateContentSnapshot(comment.Content),
                Reason = request.Reason,
                Description = request.Description?.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                isActive = true,
                isDeleted = false
            };

            await _unitOfWork.ReportRepository.AddAsync(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Comment report created. ReportId: {ReportId}, ReporterUserId: {ReporterUserId}, TargetCommentId: {TargetCommentId}, Reason: {Reason}", report.Id, request.UserId, request.CommentId, request.Reason);
            return new CreateCommentReportCommandResponse("Şikayetiniz başarıyla alındı.");
        }
    }
}
