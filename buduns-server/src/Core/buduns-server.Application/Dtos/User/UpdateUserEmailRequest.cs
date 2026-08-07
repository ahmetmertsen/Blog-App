using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Dtos.User
{
    public class UpdateUserEmailRequest
    {
        public int UserId { get; set; }
        public required string OldEmailVerificationCode { get; set; }
        public required string NewEmailVerificationCode { get; set; }
        public required string NewEmail { get; set; }
    }
}
