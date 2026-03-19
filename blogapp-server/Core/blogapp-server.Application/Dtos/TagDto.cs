using blogapp_server.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Dtos
{
    public class TagDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
