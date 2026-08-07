using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Dtos.User
{
    public class UpdateUserPasswordRequest
    {
        public required string EmailOrUsername { get; set; }
        public required string VerificationCode { get; set; }
        public required string newPassword { get; set; }
        public required string newPasswordConfirmed { get; set; }
    }
}
