using blogapp_server.Application.Dtos;
using blogapp_server.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Report.Queries.GetReports
{
    public class GetReportsRequest : IRequest<List<ReportListDto>>
    {
        public ReportStatus? Status { get; set; }
        public ReportTargetType? TargetType { get; set; }

        public int Page { get; set; } = 1;
        public int Size { get; set; } = 20;
    }
}
