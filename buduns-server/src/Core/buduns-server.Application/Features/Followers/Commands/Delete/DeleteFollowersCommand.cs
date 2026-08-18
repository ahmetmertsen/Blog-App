using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Followers.Commands.Delete
{
    public class DeleteFollowersCommand : IRequest<DeleteFollowersCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public int FollowingId { get; set; }
    }
}
