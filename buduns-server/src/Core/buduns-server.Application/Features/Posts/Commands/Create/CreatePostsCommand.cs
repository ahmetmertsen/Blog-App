using buduns_server.Application.Common.Interfaces;
using buduns_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Posts.Commands.Create
{
    public class CreatePostsCommand : IRequest<CreatePostsCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }
        public string Content { get; set; } = string.Empty;

        public List<int> TagIds { get; set; } = new();
    }
}
