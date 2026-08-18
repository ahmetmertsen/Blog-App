using buduns_server.Application.Common.Interfaces;
using MediatR;
using System.Text.Json.Serialization;

namespace buduns_server.Application.Features.Notifications.Commands.MarkAsRead
{
    public class MarkNotificationAsReadCommand : IRequest<MarkNotificationAsReadCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        public int Id { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
