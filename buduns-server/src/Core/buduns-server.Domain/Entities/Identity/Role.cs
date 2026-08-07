using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Domain.Entities.Identity
{
    public class Role : IdentityRole<int>
    {
        public ICollection<Endpoint> Endpoints { get; set; } = new List<Endpoint>();
    }
}
