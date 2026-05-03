using blogapp_server.Application.Exceptions;
using blogapp_server.Application.Repositories;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Report.Commands.ReviewReport
{
    public class ReviewReportCommandHandler : IRequestHandler<ReviewReportCommand, ReviewReportCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewReportCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ReviewReportCommandResponse> Handle(ReviewReportCommand request, CancellationToken cancellationToken)
        {
            var report = await _unitOfWork.ReportRepository.GetByIdAsync(request.ReportId);
            if (report == null)
            {
                throw new NotFoundException("Şikayet bulunamadı.");
            }

            if (report.Status != ReportStatus.Pending)
            {
                throw new BadRequestException("Bu şikayet zaten incelenmiş.");
            }
                
            if (request.Status == ReportStatus.Pending)
            {
                throw new BadRequestException("Şikayet tekrar beklemede durumuna alınamaz.");
            }
                
            if (!Enum.IsDefined(typeof(ReportStatus), request.Status))
            {
                throw new BadRequestException("Geçersiz şikayet durumu.");
            }

            report.Status = request.Status;
            report.ReviewNote = request.ReviewNote?.Trim();
            report.ReviewedByUserId = request.UserId;
            report.ReviewedDate = DateTime.UtcNow;

            _unitOfWork.ReportRepository.Update(report);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ReviewReportCommandResponse(Succeeded:true, Message:"Şikayet başarıyla güncellendi.");
        }
    }
}
