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
    public class Notification : BaseEntity
    {
        public NotificationType Type { get; set; }
        public required string Message { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int? ActorUserId { get; set; }
        public User? ActorUser { get; set; }
        public int? PostId { get; set; }
        public Post? Post { get; set; }
        public int? CommentId { get; set; }
        public Comment? Comment { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
