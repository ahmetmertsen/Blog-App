using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Exceptions
{
    public abstract class ApplicationException : Exception
    {
        public int HttpStatusCode { get; protected set; }
        public string ErrorCode { get; protected set; } = string.Empty;

        protected ApplicationException(string message, int httpStatusCode, string errorCode) : base(message)
        {
            HttpStatusCode = httpStatusCode;
            ErrorCode = errorCode;
        }
    }
}
