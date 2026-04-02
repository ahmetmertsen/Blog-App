using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos.Auth
{
    public class ChangeEmailResponse
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
    }
}
