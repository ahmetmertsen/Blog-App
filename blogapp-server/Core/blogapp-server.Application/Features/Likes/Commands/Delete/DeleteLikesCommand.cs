using blogapp_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Likes.Commands.Delete
{
    public class DeleteLikesCommand : IRequest<DeleteLikesCommandResponse>, ICurrentUserRequest
    {
        public int Id { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
