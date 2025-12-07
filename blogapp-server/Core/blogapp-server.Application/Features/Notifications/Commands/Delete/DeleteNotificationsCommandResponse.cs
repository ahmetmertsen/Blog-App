using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Notifications.Commands.Delete
{
    public record DeleteNotificationsCommandResponse(bool Succeeded,string Message)
    {
    }
}
