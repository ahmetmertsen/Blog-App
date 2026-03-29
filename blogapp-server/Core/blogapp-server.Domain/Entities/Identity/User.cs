using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Domain.Entities.Identity
{
    public class User : IdentityUser<int>
    {
        public string FullName { get; set; }
        public string? Bio { get; set; }
        public string? ImageUrl { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenEndDate { get; set; }
        public DateTime? LastPasswordChangedDate { get; set; }

        public ICollection<Post> Posts { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public ICollection<Bookmark> Bookmarks { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public ICollection<Like> Like { get; set; }

        public ICollection<Follower> Followers { get; set; }
        public ICollection<Follower> Followings { get; set; }

    }
}
