using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Exceptions
{
    public class RegisterFailedException : ApplicationException
    {
        public RegisterFailedException(string message) : base(message, 400, "REGISTER_FAILED")
        {

        }
    }
}
