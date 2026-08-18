using buduns_server.Application.Common.Interfaces;
using buduns_server.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace buduns_server.Application.Features.Report.Commands.CreateCommentReport
{
    public class CreateCommentReportCommand : IRequest<CreateCommentReportCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        public int CommentId { get; set; }
        public ReportReason Reason { get; set; }
        public string? Description { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
