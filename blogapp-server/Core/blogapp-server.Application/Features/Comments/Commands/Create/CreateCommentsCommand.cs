using blogapp_server.Application.Common.Interfaces;
using blogapp_server.Application.Features.Posts.Queries.GetAllByTagId;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Commands.Create
{
    public class CreateCommentsCommand : IRequest<CreateCommentsCommandResponse> , ICurrentUserRequest
    {
        public int PostId { get; set; }
        public string Content { get; set; } = null!;

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
