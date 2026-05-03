using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Exceptions
{
    public class ChangePhoneNumberFailedException : ApplicationException
    {
        public ChangePhoneNumberFailedException(string message)
        : base(message, 400, "PHONE_NUMBER_CHANGE_FAILED")
        {
        }
    }
}
