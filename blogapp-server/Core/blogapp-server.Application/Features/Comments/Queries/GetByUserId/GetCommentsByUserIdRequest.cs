using blogapp_server.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Queries.GetByUserId
{
    public class GetCommentsByUserIdRequest : IRequest<List<CommentDto>>
    {
        public int UserId { get; set; }
    }
}
