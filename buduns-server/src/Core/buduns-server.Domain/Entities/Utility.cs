using buduns_server.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Domain.Entities
{
    public class Utility : BaseEntity
    {
        public required string Name { get; set; }
        public required string Value { get; set; }
    }
}
