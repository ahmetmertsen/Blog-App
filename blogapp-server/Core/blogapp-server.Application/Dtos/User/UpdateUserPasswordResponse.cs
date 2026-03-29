using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos.User
{
    public class UpdateUserPasswordResponse
    {
        public bool Succedded { get; set; }
        public string Message { get; set; }
    }
}
