using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos.Auth
{
    public class ChangePhoneNumberRequest
    {
        public int UserId { get; set; }
        public string NewPhoneNumber { get; set; } = null!;
    }
}
