using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Dtos.Configurations
{
    public class Action
    {
        public required string ActionType { get; set; }
        public required string HttpType { get; set; }
        public required string Definition { get; set; }
        public required string Code { get; set; }
    }
}
