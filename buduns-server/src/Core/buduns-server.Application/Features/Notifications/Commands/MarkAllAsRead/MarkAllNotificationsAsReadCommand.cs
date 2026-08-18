using buduns_server.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace buduns_server.Application.Features.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllNotificationsAsReadCommand : IRequest<MarkAllNotificationsAsReadCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }
    }
}
