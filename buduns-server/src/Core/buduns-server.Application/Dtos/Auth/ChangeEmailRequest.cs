using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Dtos.Auth
{
    public class ChangeEmailRequest
    {
        public int UserId { get; set; }
        public required string NewEmail { get; set; }
    }
}
