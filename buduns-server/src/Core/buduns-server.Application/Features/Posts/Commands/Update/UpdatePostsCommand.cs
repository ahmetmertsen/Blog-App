using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Posts.Commands.Update
{
    public class UpdatePostsCommand : IRequest<UpdatePostsCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;

        public List<int> TagIds { get; set; } = new();

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
