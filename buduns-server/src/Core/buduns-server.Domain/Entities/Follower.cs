using buduns_server.Domain.Entities.Common;
using buduns_server.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Domain.Entities
{
    public class Follower : BaseEntity
    {
        public int FollowerId { get; set; }
        public User FollowerUser { get; set; } = null!;

        public int FollowingId { get; set; }
        public User FollowingUser { get; set; } = null!;
    }
}
