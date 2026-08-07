using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Commands.Update.UpdateMailVerify
{
    public class UpdateUserMailVerifyCommand : IRequest<UpdateUserMailVerifyCommandResponse>, ICurrentUserRequest, IAllowUnverifiedEmail
    {
        [JsonIgnore]
        public int UserId { get; set; }
        public string VerificationCode { get; set; } = string.Empty;
    }
}
