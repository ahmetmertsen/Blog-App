using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Commands.Update.UpdateProfile
{
    public class UpdateUserProfileCommand : IRequest<UpdateUserProfileCommandResponse>, ITransactionalRequest, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ImageUrl { get; set; }
    }
}
