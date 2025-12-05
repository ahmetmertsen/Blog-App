using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos
{
    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; }

        public int UserId { get; set; }
        public int PostId { get; set; }
    }
}
