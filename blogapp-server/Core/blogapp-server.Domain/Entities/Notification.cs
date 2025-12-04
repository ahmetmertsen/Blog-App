using blogapp_server.Domain.Entities.Common;
using blogapp_server.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string Type { get; set; }
        public string Message { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
