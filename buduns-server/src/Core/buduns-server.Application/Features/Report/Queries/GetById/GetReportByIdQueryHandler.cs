using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Mapping;
using buduns_server.Application.Repositories;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Report.Queries.GetById
{
    public class GetReportByIdQueryHandler : IRequestHandler<GetReportByIdQuery, ReportDetailDto>
    {
        private readonly IReportRepository _reportRepository;

        public GetReportByIdQueryHandler(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        public async Task<ReportDetailDto> Handle(GetReportByIdQuery request, CancellationToken cancellationToken)
        {
            var report = await _reportRepository.GetByIdWithDetailsAsync(request.ReportId);
            if (report == null)
            {
                throw new NotFoundException("Şikayet bulunamadı.");
            }

            var targetId = report.TargetType == ReportTargetType.Post ? report.TargetPostId : report.TargetType == ReportTargetType.User ? report.TargetUserId : report.TargetCommentId;
            if (!targetId.HasValue)
            {
                throw new BadRequestException("Şikayet hedefi bulunamadı.");
            }

            var relatedReports = await _reportRepository.GetReportsForTargetAsync(report.TargetType, targetId.Value, cancellationToken);

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
