using blogapp_server.Domain.Entities.Common;
using blogapp_server.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Domain.Entities
{
    public class Follower : BaseEntity
    {
        public int FollowerId { get; set; }
        public User FollowerUser { get; set; }

        public int FollowingId { get; set; }
        public User FollowingUser { get; set; }
    }
}
