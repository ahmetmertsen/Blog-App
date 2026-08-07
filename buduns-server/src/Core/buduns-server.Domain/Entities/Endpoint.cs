using buduns_server.Domain.Entities.Common;
using buduns_server.Domain.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Domain.Entities
{
    public class Endpoint : BaseEntity
    {
        public Endpoint()
        {
            Roles = new HashSet<Role>();
        }

        public required string ActionType { get; set; }
        public required string HttpType { get; set; }
        public required string Definition { get; set; }
        public required string Code { get; set; }

        public Menu Menu { get; set; } = null!;
        public ICollection<Role> Roles { get; set; }
    }
}
