using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
    }
}
