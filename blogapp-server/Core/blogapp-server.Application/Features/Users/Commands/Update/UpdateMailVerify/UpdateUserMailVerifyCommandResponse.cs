using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdateMailVerify
{
    public class UpdateUserMailVerifyCommandResponse
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
    }
}
