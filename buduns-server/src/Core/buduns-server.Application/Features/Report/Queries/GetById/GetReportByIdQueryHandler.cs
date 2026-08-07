using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Mapping;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Report.Queries.GetById
{
    public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, ReportDetailDto>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetReportByIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ReportDetailDto> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
        {
            var report = await _unitOfWork.ReportRepository.GetByIdWithDetailsAsync(request.ReportId);
            if (report == null)
            {
                throw new NotFoundException("Şikayet bulunamadı.");
            }

            var targetId = report.TargetType == ReportTargetType.Post ? report.TargetPostId : report.TargetType == ReportTargetType.User ? report.TargetUserId : report.TargetCommentId;
            if (!targetId.HasValue)
            {
                throw new BadRequestException("Şikayet hedefi bulunamadı.");
            }

            var relatedReports = await _unitOfWork.ReportRepository.GetReportsForTargetAsync(report.TargetType, targetId.Value, cancellationToken);

            var response = report.ToDetailDto();
            response.Priority = ReportPriorityHelper.GetHighestPriority(relatedReports.Select(relatedReport => relatedReport.Reason));
            response.ReportCount = relatedReports.Count;
            response.RelatedReports = relatedReports.ToRelatedDtoList();
            response.ModerationActions = relatedReports
                    .SelectMany(relatedReport => relatedReport.ModerationActions)
                    .OrderByDescending(action => action.CreatedAt)
                    .ToDtoList();

            return response;
        }
    }
}
