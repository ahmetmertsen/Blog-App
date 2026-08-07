using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Dtos.User
{
    public class UpdateUserMailVerifyRequest
    {
        public int UserId { get; set; }
        public required string VerificationCode { get; set; }
    }
}
