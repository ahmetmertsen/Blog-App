using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Notifications.Commands.Update
{
    public record UpdateNotificationsCommandResponse(bool Succeeded, string Message)
    {
    }
}
