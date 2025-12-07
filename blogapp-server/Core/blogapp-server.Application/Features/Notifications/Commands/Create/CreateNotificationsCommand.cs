using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Notifications.Commands.Create
{
    public record CreateNotificationsCommand(int UserId, string Type, string Message) : IRequest<CreateNotificationsCommandResponse>
    {
    }
}
