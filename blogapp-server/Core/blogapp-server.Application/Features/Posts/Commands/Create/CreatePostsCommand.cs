using blogapp_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Commands.Create
{
    public class CreatePostsCommand : IRequest<CreatePostsCommandResponse>, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }
        public string Title { get; init; }
        public string Content { get; init; }
        public string CoverImgUrl { get; init; }
        public bool isPublished { get; init; }
    }
}
