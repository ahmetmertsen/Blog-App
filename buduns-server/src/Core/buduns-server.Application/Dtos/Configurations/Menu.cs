using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Dtos.Configurations
{
    public class Menu
    {
        public required string Name { get; set; }
        public List<Action> Actions { get; set; } = new();
    }
}
