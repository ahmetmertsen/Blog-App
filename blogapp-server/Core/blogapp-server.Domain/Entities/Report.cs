using blogapp_server.Domain.Entities.Common;
using blogapp_server.Domain.Entities.Identity;
using blogapp_server.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Domain.Entities
{
    public class Report : BaseEntity
    {
        public int ReporterUserId { get; set; }

        public ReportTargetType TargetType { get; set; }
        public int? TargetPostId { get; set; }
        public int? TargetUserId { get; set; }

        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public int? ReviewedByUserId { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewNote { get; set; }

        public User ReporterUser { get; set; }
        public Post? TargetPost { get; set; }
        public User? TargetUser { get; set; }
        public User? ReviewedByUser { get; set; }
    }
}
