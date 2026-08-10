using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Features.AuthorizationEndpoint.Commands.AssignRoleEndpoint;
using buduns_server.Application.Features.AuthorizationEndpoint.Queries.GetRolesToEndpoint;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AuthorizationEndpointsController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public AuthorizationEndpointsController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }


        [AuthorizeDefinition(Menu = "Authorization Endpoints", ActionType = ActionType.Reading, Definition = "Get Roles To Endpoint")]
        [HttpPost]
        [Route("getRolesToEndpoint")]
        public async Task<ActionResult<ApiResponse<GetRolesToEndpointQueryResponse>>> GetRolesToEndpoint([FromBody] GetRolesToEndpointQuery request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [AuthorizeDefinition(Menu = "Authorization Endpoints", ActionType = ActionType.Writing, Definition = "Assign Role Endpoint")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<AssignRoleEndpointCommandResponse>>> AssignRoleEndpoint(AssignRoleEndpointCommand request)
        {
            request.Type = typeof(Program);

            var response = await _mediatR.Send(request);
            return Success(response);
        }
    }
}
