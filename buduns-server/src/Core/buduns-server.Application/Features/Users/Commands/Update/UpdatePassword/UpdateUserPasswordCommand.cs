using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Commands.Update.UpdatePassword
{
    public class UpdateUserPasswordCommand : IRequest<UpdateUserPasswordCommandResponse>
    {
        public string EmailOrUsername { get; set; } = null!;
        public string VerificationCode { get; set; } = null!;
        public string newPassword { get; set; } = null!;
        public string newPasswordConfirmed { get; set; } = null!;
    }
}
