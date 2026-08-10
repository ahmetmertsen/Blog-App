using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Likes.Commands.Create;
using buduns_server.Application.Features.Likes.Commands.Delete;
using buduns_server.Application.Features.Likes.Queries.GetByPostId;
using buduns_server.Application.Features.Likes.Queries.GetMyLikes;
using buduns_server.Application.Features.Likes.Queries.GetStatus;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class LikeController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public LikeController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Likes, ActionType = ActionType.Writing, Definition = "Create Like")]
        [HttpPost("{postId:int}")]
        public async Task<ActionResult<ApiResponse<CreateLikesCommandResponse>>> Create(int postId)
        {
            var response = await _mediatR.Send(new CreateLikesCommand { PostId = postId });
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Likes, ActionType = ActionType.Deleting, Definition = "Delete Like")]
        [HttpDelete("{postId:int}")]
        public async Task<ActionResult<ApiResponse<DeleteLikesCommandResponse>>> Delete(int postId)
        {
            var response = await _mediatR.Send(new DeleteLikesCommand { PostId = postId });
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Likes, ActionType = ActionType.Reading, Definition = "Get Like Status")]
        [HttpGet("status/{postId:int}")]
        public async Task<ActionResult<ApiResponse<GetLikeStatusQueryResponse>>> GetStatus(int postId)
        {
            var response = await _mediatR.Send(new GetLikeStatusQuery { PostId = postId });
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Likes, ActionType = ActionType.Reading, Definition = "Get Likes By Post Id")]
        [HttpGet("post/{postId:int}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<LikeDto>>>> GetByPostId(int postId, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var response = await _mediatR.Send(new GetLikesByPostIdQuery { PostId = postId, Page = page, Size = size });
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Likes, ActionType = ActionType.Reading, Definition = "Get My Liked Posts")]
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<PagedResponse<LikedPostDto>>>> GetMyLikedPosts([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var response = await _mediatR.Send(new GetMyLikedPostsQuery { Page = page, Size = size });
            return Success(response);
        }
    }
}
