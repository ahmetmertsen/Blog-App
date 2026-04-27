using blogapp_server.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Queries.GetByPostId
{
    public class GetCommentsByPostIdRequest : IRequest<List<CommentDto>>
    {
        public int PostId { get; set; }
    }
}
