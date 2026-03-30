using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos.User
{
    public class UpdateUserProfileRequest
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Bio { get; set; }
        public string ImageUrl { get; set; }
    }
}
