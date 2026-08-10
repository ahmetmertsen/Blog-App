using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Followers.Commands.Create;
using buduns_server.Application.Features.Followers.Commands.Delete;
using buduns_server.Application.Features.Followers.Queries.GetAllByUserId;
using buduns_server.Application.Features.Followers.Queries.GetStatus;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class FollowerController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public FollowerController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Followers, ActionType = ActionType.Writing, Definition = "Follow User", AccessLevel = EndpointAccessLevel.Member)]
        [HttpPost("{userId:int}")]
        public async Task<ActionResult<ApiResponse<CreateFollowersCommandResponse>>> Create(int userId)
        {
            return Success(await _mediatR.Send(new CreateFollowersCommand { FollowingId = userId }));
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Followers, ActionType = ActionType.Deleting, Definition = "Unfollow User", AccessLevel = EndpointAccessLevel.Member)]
        [HttpDelete("{userId:int}")]
        public async Task<ActionResult<ApiResponse<DeleteFollowersCommandResponse>>> Delete(int userId)
        {
            return Success(await _mediatR.Send(new DeleteFollowersCommand { FollowingId = userId }));
        }

        [HttpGet("{userId:int}/followers")]
        public async Task<ActionResult<ApiResponse<PagedResponse<FollowerDto>>>> GetFollowersByUserId(int userId, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            return Success(await _mediatR.Send(new GetAllFollowersByUserIdQuery { UserId = userId, Page = page, Size = size }));
        }

        [HttpGet("{userId:int}/followings")]
        public async Task<ActionResult<ApiResponse<PagedResponse<FollowerDto>>>> GetFollowingsByUserId(int userId, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            return Success(await _mediatR.Send(new GetAllFollowingsByUserIdQuery { UserId = userId, Page = page, Size = size }));
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Followers, ActionType = ActionType.Reading, Definition = "Get Follow Status", AccessLevel = EndpointAccessLevel.Member)]
        [HttpGet("status/{userId:int}")]
        public async Task<ActionResult<ApiResponse<GetFollowerStatusQueryResponse>>> GetStatus(int userId)
        {
            return Success(await _mediatR.Send(new GetFollowerStatusQuery { FollowingId = userId }));
        }
    }
}
