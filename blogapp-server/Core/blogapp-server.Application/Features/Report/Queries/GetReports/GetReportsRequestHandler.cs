using blogapp_server.Application.Dtos;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Report.Queries.GetReports
{
    public class GetReportsRequestHandler : IRequestHandler<GetReportsRequest, List<ReportListDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetReportsRequestHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ReportListDto>> Handle(GetReportsRequest request, CancellationToken cancellationToken)
        {
            var reports = await _unitOfWork.ReportRepository.GetFilteredReportsAsync(request.Status, request.TargetType, request.Page, request.Size);

            return reports.Select(r => new ReportListDto
            {
                Id = r.Id,
                ReporterUserId = r.ReporterUserId,
                ReporterUserName = r.ReporterUser?.UserName,
                ReporterFullName = r.ReporterUser?.FullName,
                
                TargetType = r.TargetType,
                TargetPostId = r.TargetPostId,
                TargetPostTitle = r.TargetPost?.Title,
                
                TargetUserId = r.TargetUserId,
                TargetUserName = r.TargetUser?.UserName,
                TargetUserFullName = r.TargetUser?.FullName,

                Reason = r.Reason,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
            }).ToList();
        }
    }
}
