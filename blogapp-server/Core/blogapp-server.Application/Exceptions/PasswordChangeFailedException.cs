using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Exceptions
{
    public class PasswordChangeFailedException : ApplicationException
    {
        public PasswordChangeFailedException(string message)
        : base(message, 400, "PASSWORD_CHANGE_FAILED")
        {
        }
    }
}
