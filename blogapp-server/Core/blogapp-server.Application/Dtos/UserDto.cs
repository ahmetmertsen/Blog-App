using blogapp_server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos
{
    public class UserDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Bio { get; set; }
        public string ImageUrl { get; set; }

        public ICollection<Post> Posts { get; set; }
        public ICollection<Follower> Followers { get; set; }
        public ICollection<Follower> Followings { get; set; }

    }
}
