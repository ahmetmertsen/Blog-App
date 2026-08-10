using buduns_server.Application.Features.Tags.Commands.Create;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Tags.Commands.Delete;
using buduns_server.Application.Features.Tags.Commands.Update;
using buduns_server.Application.Features.Tags.Queries.GetAll;
using buduns_server.Application.Features.Tags.Queries.GetById;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class TagController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public TagController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Tags, ActionType = ActionType.Writing, Definition = "Create Tag", AccessLevel = EndpointAccessLevel.Member)]
        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<ApiResponse<CreateTagsCommandResponse>>> Create([FromBody] CreateTagsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Tags, ActionType = ActionType.Updating, Definition = "Update Tag", AccessLevel = EndpointAccessLevel.Member)]
        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<ApiResponse<UpdateTagsCommandResponse>>> Update([FromBody] UpdateTagsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Tags, ActionType = ActionType.Deleting, Definition = "Delete Tag", AccessLevel = EndpointAccessLevel.Member)]
        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult<ApiResponse<DeleteTagsCommandResponse>>> Delete([FromBody] DeleteTagsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [HttpGet]
        [Route("getAll")]
        public async Task<ActionResult<ApiResponse<PagedResponse<TagDto>>>> GetAll([FromQuery] int page = 1, [FromQuery] int size = 50, [FromQuery] string? search = null)
        {
            var response = await _mediatR.Send(new GetAllTagsQuery { Page = page, Size = size, Search = search });
            return Success(response);
        }

        [HttpGet]
        [Route("getById/{id}")]
        public async Task<ActionResult<ApiResponse<TagDto>>> GetById(int id)
        {
            var response = await _mediatR.Send(new GetTagByIdQuery(id));
            return Success(response);
        }
    }
}
