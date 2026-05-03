using blogapp_server.Application.Features.Report.Commands.CreatePostReport;
using blogapp_server.Application.Features.Report.Commands.CreateUserReport;
using blogapp_server.Application.Features.Report.Commands.ReviewReport;
using blogapp_server.Application.Features.Report.Queries.GetById;
using blogapp_server.Application.Features.Report.Queries.GetReports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace blogapp_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IMediator _mediatR;

        public ReportController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [Authorize]
        [HttpPost("createPostReport")]
        public async Task<IActionResult> CreatePostReport([FromBody] CreatePostReportCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("createUserReport")]
        public async Task<IActionResult> CreateUserReport([FromBody] CreateUserReportCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetReports([FromQuery] GetReportsRequest request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpGet]
        [Route("getById/{reportId}")]
        public async Task<IActionResult> GetReportById(int reportId)
        {
            var response = await _mediatR.Send(new GetReportByIdRequest { ReportId = reportId });
            return Ok(response);
        }

        [Authorize]
        [HttpPost("review")]
        public async Task<IActionResult> ReviewReport([FromBody] ReviewReportCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }
    }
}
