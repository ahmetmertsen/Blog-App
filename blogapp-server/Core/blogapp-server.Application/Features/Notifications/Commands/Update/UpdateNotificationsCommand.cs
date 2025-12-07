using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Notifications.Commands.Update
{
    public record UpdateNotificationsCommand(int Id, string Type, string Message) : IRequest<UpdateNotificationsCommandResponse> 
    {
    }
}
