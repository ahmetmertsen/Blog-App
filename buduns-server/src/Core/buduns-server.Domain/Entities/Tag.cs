using buduns_server.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Domain.Entities
{
    public class Tag : BaseEntity
    {
        public required string Name { get; set; }
        public required string NormalizedName { get; set; }

        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
