using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Comments.Commands.Update
{
    public class UpdateCommentsCommand : IRequest<UpdateCommentsCommandResponse>, ICurrentUserRequest
    {
        [JsonIgnore]
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
