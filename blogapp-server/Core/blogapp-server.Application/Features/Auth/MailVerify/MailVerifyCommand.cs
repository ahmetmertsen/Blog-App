using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Auth.MailVerify
{
    public class MailVerifyCommand : IRequest<MailVerifyCommandResponse>
    {
        public string Email { get; set; }
    }
}
