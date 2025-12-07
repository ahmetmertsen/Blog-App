using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Followers.Commands.Create
{
    public record CreateFollowersCommand(int FollowerId, int FollowingId) : IRequest<CreateFollowersCommandResponse>
    {
    }
}
