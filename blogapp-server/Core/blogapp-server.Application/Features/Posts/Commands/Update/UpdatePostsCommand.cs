using blogapp_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Commands.Update
{
    public class UpdatePostsCommand : IRequest<UpdatePostsCommandResponse>, ICurrentUserRequest
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string CoverImgUrl { get; set; }
        public bool isPublished { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
