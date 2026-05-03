using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Report.Commands.CreatePostReport
{
    public record CreatePostReportCommandResponse(bool Succeeded, string Message)
    {
    }
}
