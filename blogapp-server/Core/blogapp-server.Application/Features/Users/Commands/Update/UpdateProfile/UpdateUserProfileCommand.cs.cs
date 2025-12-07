using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdateProfile
{
    public record UpdateUserProfileCommand(string Id, string UserName ,string FullName, string Bio, string ImageUrl) : IRequest<UpdateUserProfileCommandResponse>
    {
    }
}
