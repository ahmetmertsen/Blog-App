using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Report.Commands.ReviewReport
{
    public record ReviewReportCommandResponse(bool Succeeded, string Message)
    {
    }
}
