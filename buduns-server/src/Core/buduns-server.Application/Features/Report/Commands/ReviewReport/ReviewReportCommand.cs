using buduns_server.Application.Common.Interfaces;
using buduns_server.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Report.Commands.ReviewReport
{
    public class ReviewReportCommand : IRequest<ReviewReportCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public int ReportId { get; set; }
        public ReportStatus Status { get; set; }
        public ModerationActionType ActionType { get; set; }
        public int? SuspensionDays { get; set; }
        public string? ReviewNote { get; set; }
    }
}
