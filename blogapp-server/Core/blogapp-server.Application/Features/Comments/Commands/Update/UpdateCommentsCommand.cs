using blogapp_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Commands.Update
{
    public class UpdateCommentsCommand : IRequest<UpdateCommentsCommandResponse>, ICurrentUserRequest
    {
        public int Id { get; set; }
        public string Content { get; set; }

        [JsonIgnore]
        public int UserId { get; set; }
    }
}
