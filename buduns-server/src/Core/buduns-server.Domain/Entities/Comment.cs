using buduns_server.Domain.Entities.Common;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Domain.Entities
{
    public class Comment : BaseEntity
    {
        public required string Content { get; set; }
        public CommentStatus Status { get; set; } = CommentStatus.Published;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public ICollection<Report> Reports { get; set; } = new List<Report>();
        public ICollection<ModerationAction> ModerationActions { get; set; } = new List<ModerationAction>();
    }
}
