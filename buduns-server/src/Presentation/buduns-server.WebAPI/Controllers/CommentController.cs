using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Comments.Commands.Create;
using buduns_server.Application.Features.Comments.Commands.Delete;
using buduns_server.Application.Features.Comments.Commands.Update;
using buduns_server.Application.Features.Comments.Queries.GetById;
using buduns_server.Application.Features.Comments.Queries.GetByPostId;
using buduns_server.Application.Features.Comments.Queries.GetByUserId;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class CommentController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public CommentController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Comments, ActionType = ActionType.Writing, Definition = "Create Comment")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CreateCommentsCommandResponse>>> Create([FromBody] CreateCommentsCommand request)
        {
            return Success(await _mediatR.Send(request));
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Comments, ActionType = ActionType.Updating, Definition = "Update Comment")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<UpdateCommentsCommandResponse>>> Update(int id, [FromBody] UpdateCommentsCommand request)
        {
            request.Id = id;
            return Success(await _mediatR.Send(request));
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Comments, ActionType = ActionType.Deleting, Definition = "Delete Comment")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<DeleteCommentsCommandResponse>>> Delete(int id)
        {
            return Success(await _mediatR.Send(new DeleteCommentsCommand { Id = id }));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<CommentDto>>> GetById(int id)
        {
            return Success(await _mediatR.Send(new GetCommentByIdQuery(id)));
        }

        [HttpGet("post/{postId:int}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<CommentDto>>>> GetByPostId(int postId, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            return Success(await _mediatR.Send(new GetCommentsByPostIdQuery { PostId = postId, Page = page, Size = size }));
        }

        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<CommentDto>>>> GetByUserId(int userId, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            return Success(await _mediatR.Send(new GetCommentsByUserIdQuery { UserId = userId, Page = page, Size = size }));
        }
    }
}
