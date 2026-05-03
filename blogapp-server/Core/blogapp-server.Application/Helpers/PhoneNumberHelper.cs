using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Helpers
{
    public static class PhoneNumberHelper
    {
        public static string NormalizeTurkeyPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return string.Empty;

            string normalized = phoneNumber
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "");

            if (normalized.StartsWith("0"))
            {
                normalized = "+90" + normalized.Substring(1);
            }
            else if (normalized.StartsWith("5"))
            {
                normalized = "+90" + normalized;
            }
            else if (normalized.StartsWith("90"))
            {
                normalized = "+" + normalized;
            }

            return normalized;
        }
    }
}
