using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Exceptions
{
    public class NotFoundException : ApplicationException
    {
        public NotFoundException(string message)
            : base(message, 404, "RESOURCE_NOT_FOUND")
        {
        }
    }
}
