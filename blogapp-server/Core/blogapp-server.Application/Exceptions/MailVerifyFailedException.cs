using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Exceptions
{
    public class MailVerifyFailedException : ApplicationException
    {
        public MailVerifyFailedException(string message)
        : base(message, 400, "MAIL_VERIFY_FAILED")
        {
        }
    }
}
