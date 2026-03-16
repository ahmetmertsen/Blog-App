using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Exceptions
{
    public class UnauthorizedAccesException : ApplicationException
    {
        public UnauthorizedAccesException(string message)
            : base(message, 401, "UNAUTHORIZED_ACCESS")
        {
        }
    }
}
