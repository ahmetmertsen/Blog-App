using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Bookmarks.Commands.Create
{
    public class CreateBookmarksCommand : IRequest<CreateBookmarksCommandResponse>, ITransactionalRequest , ICurrentUserRequest
    {
        public int PostId { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
