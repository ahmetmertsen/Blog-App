using blogapp_server.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Domain.Entities
{
    public class Tag : BaseEntity
    {
        public string Name { get; set; }

        public ICollection<Post> Posts { get; set; }
    }
}
