using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Domain.Enums
{
    public enum ReportReason
    {
        Spam = 1,
        Harassment,
        HateSpeech,
        Violence,
        SexualContent,
        Misinformation,
        Copyright,
        Other = 99
    }
}
