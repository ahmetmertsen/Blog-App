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

        public ICollection<PostDto> Posts { get; set; }
        public ICollection<FollowerDto> Followers { get; set; }
        public ICollection<FollowerDto> Followings { get; set; }

    }
}
