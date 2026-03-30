using blogapp_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdateProfile
{
    public class UpdateUserProfileCommand : IRequest<UpdateUserProfileCommandResponse>, ICurrentUserRequest
    {
        [JsonIgnore]
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Bio { get; set; }
        public string ImageUrl { get; set; }
    }
}
