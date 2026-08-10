using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos.Role;
using buduns_server.Application.Features.Roles.Commands.Create;
using buduns_server.Application.Features.Roles.Commands.Delete;
using buduns_server.Application.Features.Roles.Commands.Update;
using buduns_server.Application.Features.Roles.Queries.GetAll;
using buduns_server.Application.Features.Roles.Queries.GetById;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class RoleController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public RoleController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [AuthorizeDefinition(Menu = AuthorizeDefinitionConstants.Roles, ActionType = ActionType.Reading, Definition = "GetAll Roles")]
        [HttpGet]
        [Route("getAll")]
        public async Task<ActionResult<ApiResponse<List<RoleDto>>>> GetAll()
        {
            var response = await _mediatR.Send(new GetAllRolesQuery());
            return Success(response);
        }

        [AuthorizeDefinition(Menu = AuthorizeDefinitionConstants.Roles, ActionType = ActionType.Reading, Definition = "Get Role By Id")]
        [HttpGet]
        [Route("getById/{id}")]
        public async Task<ActionResult<ApiResponse<RoleDto>>> GetById(int id)
        {
            var response = await _mediatR.Send(new GetRoleByIdQuery { Id = id });
            return Success(response);
        }

        [AuthorizeDefinition(Menu = AuthorizeDefinitionConstants.Roles, ActionType = ActionType.Writing, Definition = "Create Role")]
        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<ApiResponse<CreateRoleCommandResponse>>> Create([FromBody] CreateRoleCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [AuthorizeDefinition(Menu = AuthorizeDefinitionConstants.Roles, ActionType = ActionType.Updating, Definition = "Update Role")]
        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<ApiResponse<UpdateRoleCommandResponse>>> Update([FromBody] UpdateRoleCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [AuthorizeDefinition(Menu = AuthorizeDefinitionConstants.Roles, ActionType = ActionType.Deleting, Definition = "Delete Role")]
        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<ActionResult<ApiResponse<DeleteRoleCommandResponse>>> Delete(int id)
        {
            var response = await _mediatR.Send(new DeleteRoleCommand { Id = id });
            return Success(response);
        }
    }
}
