using blogapp_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.ChangePhoneNumber
{
    public class ChangePhoneNumberCommand : IRequest<ChangePhoneNumberCommandResponse> , ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }

        public string NewPhoneNumber { get; set; } = null!;
    }
}
