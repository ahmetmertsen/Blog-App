using blogapp_server.Application.Exceptions;
using blogapp_server.Application.Repositories;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities.Identity;
using blogapp_server.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Report.Commands.CreateUserReport
{
    public class CreateUserReportCommandHandler : IRequestHandler<CreateUserReportCommand, CreateUserReportCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;

        public CreateUserReportCommandHandler(IUnitOfWork unitOfWork, UserManager<User> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<CreateUserReportCommandResponse> Handle(CreateUserReportCommand request, CancellationToken cancellationToken)
        {   
            if (request.UserId == request.TargetUserId)
            {
                throw new BadRequestException("Kendinizi şikayet edemezsiniz.");
            }
                
            User? targetUser = await _userManager.FindByIdAsync(request.TargetUserId.ToString());
            if (targetUser == null)
            {
                throw new NotFoundException("Şikayet edilen kullanıcı bulunamadı.");
            }
                

            bool alreadyReported = await _unitOfWork.ReportRepository.HasPendingUserReportAsync(request.UserId, request.TargetUserId);
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
                Reason = request.Reason,
                Description = request.Description?.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ReportRepository.AddAsync(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateUserReportCommandResponse(Succeeded:true, Message:"Şikayetiniz başarıyla alındı.");
        }

    }
}
