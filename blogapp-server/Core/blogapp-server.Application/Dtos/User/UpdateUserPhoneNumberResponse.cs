using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos.User
{
    public class UpdateUserPhoneNumberResponse
    {
        public bool Succeeded { get; set; }
        public string Message { get; set; }
    }
}
