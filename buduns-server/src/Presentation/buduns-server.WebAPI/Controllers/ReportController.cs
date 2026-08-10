using buduns_server.Application.Features.Report.Commands.CreatePostReport;
using buduns_server.Application.Features.Report.Commands.CreateCommentReport;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Report.Commands.CreateUserReport;
using buduns_server.Application.Features.Report.Commands.ReviewReport;
using buduns_server.Application.Features.Report.Queries.GetById;
using buduns_server.Application.Features.Report.Queries.GetReports;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class ReportController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public ReportController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Reports, ActionType = ActionType.Writing, Definition = "Create Post Report")]
        [HttpPost("createPostReport")]
        public async Task<ActionResult<ApiResponse<CreatePostReportCommandResponse>>> CreatePostReport([FromBody] CreatePostReportCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Reports, ActionType = ActionType.Writing, Definition = "Create Comment Report")]
        [HttpPost("createCommentReport")]
        public async Task<ActionResult<ApiResponse<CreateCommentReportCommandResponse>>> CreateCommentReport([FromBody] CreateCommentReportCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Reports, ActionType = ActionType.Writing, Definition = "Create User Report")]
        [HttpPost("createUserReport")]
        public async Task<ActionResult<ApiResponse<CreateUserReportCommandResponse>>> CreateUserReport([FromBody] CreateUserReportCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize(Roles = "Admin,Moderator")]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Reports, ActionType = ActionType.Reading, Definition = "Get Reports")]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<ReportListDto>>>> GetReports([FromQuery] GetReportsQuery request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize(Roles = "Admin,Moderator")]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Reports, ActionType = ActionType.Reading, Definition = "Get Report By Id")]
        [HttpGet]
        [Route("getById/{reportId}")]
        public async Task<ActionResult<ApiResponse<ReportDetailDto>>> GetReportById(int reportId)
        {
            var response = await _mediatR.Send(new GetReportByIdQuery { ReportId = reportId });
            return Success(response);
        }

        [Authorize(Roles = "Admin,Moderator")]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Reports, ActionType = ActionType.Updating, Definition = "Review Report")]
        [HttpPost("review")]
        public async Task<ActionResult<ApiResponse<ReviewReportCommandResponse>>> ReviewReport([FromBody] ReviewReportCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }
    }
}
