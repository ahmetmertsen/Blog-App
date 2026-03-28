using blogapp_server.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Domain.Entities
{
    public class Utility : BaseEntity
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
