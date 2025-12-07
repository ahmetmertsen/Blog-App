using blogapp_server.Application.Dtos;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Queries.GetAll
{
    public record GetAllCommentsRequest : IRequest<List<CommentDto>>
    {
    }
}
