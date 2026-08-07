using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Dtos
{
    public class CommentDto
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? UserImageUrl { get; set; }
        public int PostId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
    }
}
