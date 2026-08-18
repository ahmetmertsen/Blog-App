using buduns_server.Application.Exceptions;
using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Report.Commands.CreatePostReport
{
    public class CreatePostReportCommandHandler : IRequestHandler<CreatePostReportCommand, CreatePostReportCommandResponse>
    {
        private readonly IPostRepository _postRepository;
        private readonly IReportRepository _reportRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreatePostReportCommandHandler> _logger;
        private readonly ReportPolicyOptions _reportPolicyOptions;

        public CreatePostReportCommandHandler(IPostRepository postRepository, IReportRepository reportRepository, IUnitOfWork unitOfWork, ILogger<CreatePostReportCommandHandler> logger, IOptions<ReportPolicyOptions> reportPolicyOptions)
        {
            _postRepository = postRepository;
            _reportRepository = reportRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _reportPolicyOptions = reportPolicyOptions.Value;
        }

        public async Task<CreatePostReportCommandResponse> Handle(CreatePostReportCommand request, CancellationToken cancellationToken)
        {
            Post? post = await _postRepository.GetByIdAsync(request.PostId);
            if (post == null)
            {
                throw new NotFoundException("Gönderi bulunamadı.");
            }

            if (post.UserId == request.UserId)
            {
                throw new BadRequestException("Kendi gönderinizi şikayet edemezsiniz.");
            }

            // Gorunurluk kontrolu burada yok: PostRepository.GetByIdAsync
            // yalnizca gorunur paylasimlari donduruyor, gizlenmis bir paylasim
            // yukaridaki NotFound dalinda kaliyor.
            var recentReportCount = await _reportRepository.CountRecentReportsByUserAsync(request.UserId, DateTime.UtcNow.AddHours(-24), cancellationToken);
            var dailyReportLimit = Math.Max(1, _reportPolicyOptions.DailyReportLimit);
            if (recentReportCount >= dailyReportLimit)
            {
                throw new TooManyRequestsException($"24 saat içinde en fazla {dailyReportLimit} şikayet oluşturabilirsiniz.");
            }
                
            bool alreadyReported = await _reportRepository.HasPendingPostReportAsync(request.UserId, request.PostId, cancellationToken);
            if (alreadyReported)
            {
                throw new BadRequestException("Bu gönderi için zaten bekleyen bir şikayetiniz var.");
            }
                
            Domain.Entities.Report report = new()
            {
                ReporterUserId = request.UserId,
                TargetType = ReportTargetType.Post,
                TargetPostId = request.PostId,
                TargetUserId = null,
                TargetOwnerUserId = post.UserId,
                TargetOwnerUserNameSnapshot = post.User?.UserName,
                TargetOwnerFullNameSnapshot = post.User?.FullName,
                TargetContentSnapshot = ReportSnapshotHelper.CreateContentSnapshot(post.Content),
                Reason = request.Reason,
                Description = request.Description?.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post report created. ReportId: {ReportId}, ReporterUserId: {ReporterUserId}, TargetPostId: {TargetPostId}, Reason: {Reason}", report.Id, request.UserId, request.PostId, request.Reason);

            return new CreatePostReportCommandResponse(Message:"Şikayetiniz başarıyla alındı.");
        }
    }
}
