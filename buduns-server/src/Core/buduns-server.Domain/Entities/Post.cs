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
    public class Post : BaseEntity
    {
        public string Content { get; set; } = string.Empty;
        public bool isPublished { get; set; }
        public PostStatus Status { get; set; } = PostStatus.Published;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();

    }
}
