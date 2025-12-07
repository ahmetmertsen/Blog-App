using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Commands.Create
{
    public record CreatePostsCommand(int UserId, string Title, string Content, string CoverImgUrl, bool isPublished) : IRequest<CreatePostsCommandResponse>
    {
    }
}
