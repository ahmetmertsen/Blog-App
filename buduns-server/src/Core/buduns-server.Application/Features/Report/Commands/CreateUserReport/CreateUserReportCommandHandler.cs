using buduns_server.Application.Exceptions;
using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Common.Options;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Report.Commands.CreateUserReport
{
    public class CreateUserReportCommandHandler : IRequestHandler<CreateUserReportCommand, CreateUserReportCommandResponse>
    {
        private readonly IReportRepository _reportRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreateUserReportCommandHandler> _logger;
        private readonly ReportPolicyOptions _reportPolicyOptions;

        public CreateUserReportCommandHandler(IReportRepository reportRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, ILogger<CreateUserReportCommandHandler> logger, IOptions<ReportPolicyOptions> reportPolicyOptions)
        {
            _reportRepository = reportRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _reportPolicyOptions = reportPolicyOptions.Value;
        }

        public async Task<CreateUserReportCommandResponse> Handle(CreateUserReportCommand request, CancellationToken cancellationToken)
        {   
            if (request.UserId == request.TargetUserId)
            {
                throw new BadRequestException("Kendinizi şikayet edemezsiniz.");
            }
                
            User? targetUser = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
            if (targetUser == null)
            {
                throw new NotFoundException("Şikayet edilen kullanıcı bulunamadı.");
            }

            if (targetUser.Status == UserStatus.Banned)
            {
                throw new BadRequestException("Bu kullanıcı zaten platformdan yasaklanmış.");
            }

            var recentReportCount = await _reportRepository.CountRecentReportsByUserAsync(request.UserId, DateTime.UtcNow.AddHours(-24), cancellationToken);
            var dailyReportLimit = Math.Max(1, _reportPolicyOptions.DailyReportLimit);
            if (recentReportCount >= dailyReportLimit)
            {
                throw new TooManyRequestsException($"24 saat içinde en fazla {dailyReportLimit} şikayet oluşturabilirsiniz.");
            }
                

            bool alreadyReported = await _reportRepository.HasPendingUserReportAsync(request.UserId, request.TargetUserId, cancellationToken);
            if (alreadyReported)
            {
                throw new BadRequestException("Bu kullanıcı için zaten bekleyen bir şikayetiniz var.");
            }
                

            Domain.Entities.Report report = new()
            {
                ReporterUserId = request.UserId,
                TargetType = ReportTargetType.User,
                TargetPostId = null,
                TargetUserId = request.TargetUserId,
                TargetOwnerUserId = targetUser.Id,
                TargetOwnerUserNameSnapshot = targetUser.UserName,
                TargetOwnerFullNameSnapshot = targetUser.FullName,
                TargetContentSnapshot = ReportSnapshotHelper.CreateContentSnapshot(targetUser.Bio),
                Reason = request.Reason,
                Description = request.Description?.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _reportRepository.AddAsync(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User report created. ReportId: {ReportId}, ReporterUserId: {ReporterUserId}, TargetUserId: {TargetUserId}, Reason: {Reason}", report.Id, request.UserId, request.TargetUserId, request.Reason);

            return new CreateUserReportCommandResponse(Message:"Şikayetiniz başarıyla alındı.");
        }

    }
}
