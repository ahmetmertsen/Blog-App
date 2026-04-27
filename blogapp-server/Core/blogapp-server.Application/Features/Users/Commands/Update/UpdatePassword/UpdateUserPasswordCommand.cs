using blogapp_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdatePassword
{
    public class UpdateUserPasswordCommand : IRequest<UpdateUserPasswordCommandResponse>
    {
        public int UserId { get; set; }
        public string ResetToken { get; set; } = null!;
        public string newPassword { get; set; } = null!;
        public string newPasswordConfirmed { get; set; } = null!;
    }
}
