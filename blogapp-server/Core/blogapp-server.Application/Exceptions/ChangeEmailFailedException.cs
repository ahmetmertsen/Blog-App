using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Exceptions
{
    public class ChangeEmailFailedException : ApplicationException
    {
        public ChangeEmailFailedException(string message)
        : base(message, 400, "EMAIL_CHANGE_FAILED")
        {
        }
    }
}
