using blogapp_server.Application.Common.Interfaces;
using blogapp_server.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Report.Commands.ReviewReport
{
    public class ReviewReportCommand : IRequest<ReviewReportCommandResponse>, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public int ReportId { get; set; }
        public ReportStatus Status { get; set; }
        public string? ReviewNote { get; set; }
    }
}
