using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Commands.Update.UpdateEmail
{
    public class UpdateUserEmailCommand : IRequest<UpdateUserEmailCommandResponse>, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }
        public string OldEmailVerificationCode { get; set; } = string.Empty;
        public string NewEmailVerificationCode { get; set; } = string.Empty;

        public string NewEmail { get; set; } = string.Empty;
    }
}
