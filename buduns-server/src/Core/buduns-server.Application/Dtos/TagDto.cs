using buduns_server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Dtos
{
    public class TagDto
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int PostCount { get; set; }
    }
}
