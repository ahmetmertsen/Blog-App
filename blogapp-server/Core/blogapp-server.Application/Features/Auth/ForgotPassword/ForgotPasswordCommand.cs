using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.ForgotPassword
{
    public class ForgotPasswordCommand : IRequest<ForgotPasswordCommandResponse>
    {
        public string EmailOrUsername { get; set; }
    }
}
