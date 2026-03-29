using blogapp_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdateMailVerify
{
    public class UpdateUserMailVerifyCommand : IRequest<UpdateUserMailVerifyCommandResponse>, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }
        public string EmailConfirmToken { get; set; }
    }
}
