using blogapp_server.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Likes.Queries.GetByUserId
{
    public class GetLikesByUserIdRequest : IRequest<List<LikeDto>>
    {
        public int UserId { get; set; }
    }
}
