using buduns_server.Application.Common.Interfaces;
using buduns_server.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Report.Commands.CreateUserReport
{
    public class CreateUserReportCommand : IRequest<CreateUserReportCommandResponse>, ITransactionalRequest , ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public int TargetUserId { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }
    }
}
