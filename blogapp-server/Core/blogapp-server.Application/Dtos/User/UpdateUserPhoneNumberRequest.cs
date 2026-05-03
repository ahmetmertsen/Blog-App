using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos.User
{
    public class UpdateUserPhoneNumberRequest
    {
        public int UserId { get; set; }
        public string NewPhoneNumber { get; set; } = null!;
        public string PhoneNumberChangeToken { get; set; } = null!;
    }
}
