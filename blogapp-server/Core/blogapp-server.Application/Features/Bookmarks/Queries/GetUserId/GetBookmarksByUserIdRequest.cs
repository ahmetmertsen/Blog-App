using blogapp_server.Application.Common.Interfaces;
using blogapp_server.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Bookmarks.Queries.GetUserId
{
    public class GetBookmarksByUserIdRequest : IRequest<List<BookmarkDto>>
    {
        public int UserId { get; set; }
    }
}
