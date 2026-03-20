using blogapp_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Followers.Commands.Create
{
    public class CreateFollowersCommand : IRequest<CreateFollowersCommandResponse>, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public int FollowingId { get; set; }
    }
}
