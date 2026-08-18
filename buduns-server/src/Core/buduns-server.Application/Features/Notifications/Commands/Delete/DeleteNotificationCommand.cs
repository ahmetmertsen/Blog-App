using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Notifications.Commands.Delete
{
    public class DeleteNotificationCommand : IRequest<DeleteNotificationCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        public int Id { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
