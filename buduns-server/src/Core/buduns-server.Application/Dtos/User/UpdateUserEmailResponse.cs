using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Dtos.User
{
    public class UpdateUserEmailResponse
    {
        public bool Succeeded { get; set; }
        public required string Message { get; set; }
    }
}
