using buduns_server.Application.Common.Helpers;
using buduns_server.Application.Dtos;
using buduns_server.Application.Mapping;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Report.Queries.GetReports
{
    public class GetReportsQueryHandler : IRequestHandler<GetReportsQuery, PagedResponse<ReportListDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetReportsQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<ReportListDto>> Handle(GetReportsQuery request, CancellationToken cancellationToken)
        {
            var (reports, totalCount) = await _unitOfWork.ReportRepository.GetFilteredReportGroupsAsync(request.Status, request.TargetType, request.Reason, request.FromDate, request.ToDate, request.Page, request.Size, cancellationToken);

            var items = reports
                .GroupBy(report => new
                {
                    report.TargetType,
                    TargetId = report.TargetType == ReportTargetType.Post ? report.TargetPostId : report.TargetType == ReportTargetType.User ? report.TargetUserId : report.TargetCommentId
                })
                .Select(group =>
                {
                    var latestReport = group.OrderByDescending(report => report.CreatedAt).First();
                    var item = latestReport.ToListDto();

                    item.TargetPostContentPreview = CreateContentPreview(latestReport.TargetPost?.Content ?? latestReport.TargetContentSnapshot);
                    item.TargetCommentContentPreview = CreateContentPreview(latestReport.TargetComment?.Content ?? latestReport.TargetContentSnapshot);
                    item.Priority = ReportPriorityHelper.GetHighestPriority(group.Select(report => report.Reason));
                    item.ReasonCounts = group
                        .GroupBy(report => report.Reason)
                        .ToDictionary(reasonGroup => reasonGroup.Key, reasonGroup => reasonGroup.Count());
                    item.ReportCount = group.Count();
                    item.FirstReportDate = group.Min(report => report.CreatedAt);
                    item.LastReportDate = group.Max(report => report.CreatedAt);

                    return item;
                })
                .OrderByDescending(item => item.Priority)
                .ThenByDescending(item => item.LastReportDate)
                .ToList();

            return new PagedResponse<ReportListDto>
            {
                Items = items,
                Page = request.Page,
                Size = request.Size,
                TotalCount = totalCount
            };
        }

        private static string? CreateContentPreview(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            const int previewLength = 160;
            var normalizedContent = content.Trim();
            return normalizedContent.Length <= previewLength ? normalizedContent : $"{normalizedContent[..previewLength]}...";
        }
    }
}
