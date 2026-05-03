using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Domain.Enums
{
    public enum ReportStatus
    {
        Pending = 1,
        Reviewed,
        Rejected,
        ActionTaken
    }
}
