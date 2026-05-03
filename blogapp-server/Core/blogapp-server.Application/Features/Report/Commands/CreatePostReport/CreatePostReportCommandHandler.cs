using blogapp_server.Application.Exceptions;
using blogapp_server.Application.Repositories;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using blogapp_server.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Report.Commands.CreatePostReport
{
    public class CreatePostReportCommandHandler : IRequestHandler<CreatePostReportCommand, CreatePostReportCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreatePostReportCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CreatePostReportCommandResponse> Handle(CreatePostReportCommand request, CancellationToken cancellationToken)
        {
            Post? post = await _unitOfWork.PostRepository.GetByIdAsync(request.PostId);
            if (post == null)
            {
                throw new NotFoundException("Gönderi bulunamadı.");
            }
                
            bool alreadyReported = await _unitOfWork.ReportRepository.HasPendingPostReportAsync(request.UserId, request.PostId);
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
                Reason = request.Reason,
                Description = request.Description?.Trim(),
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ReportRepository.AddAsync(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreatePostReportCommandResponse(Succeeded:true, Message:"Şikayetiniz başarıyla alındı.");
        }
    }
}
