using blogapp_server.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos
{
    public class ReportListDto
    {
        public int Id { get; set; }
        public int ReporterUserId { get; set; }
        public string? ReporterUserName { get; set; }
        public string? ReporterFullName { get; set; }

        public ReportTargetType TargetType { get; set; }

        public int? TargetPostId { get; set; }
        public string? TargetPostTitle { get; set; }

        public int? TargetUserId { get; set; }
        public string? TargetUserName { get; set; }
        public string? TargetUserFullName { get; set; }

        public ReportReason Reason { get; set; }
        public ReportStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
