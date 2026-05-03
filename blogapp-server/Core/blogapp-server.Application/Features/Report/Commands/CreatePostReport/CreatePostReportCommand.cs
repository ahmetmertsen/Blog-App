using blogapp_server.Application.Common.Interfaces;
using blogapp_server.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Report.Commands.CreatePostReport
{
    public class CreatePostReportCommand : IRequest<CreatePostReportCommandResponse>, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public int PostId { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
    }
}
