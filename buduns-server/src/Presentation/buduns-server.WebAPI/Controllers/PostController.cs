using buduns_server.Application.Features.Posts.Commands.Create;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Posts.Commands.Delete;
using buduns_server.Application.Features.Posts.Commands.Update;
using buduns_server.Application.Features.Posts.Queries.GetAll;
using buduns_server.Application.Features.Posts.Queries.GetDailyTopPosts;
using buduns_server.Application.Features.Posts.Queries.GetAllByTagId;
using buduns_server.Application.Features.Posts.Queries.GetById;
using buduns_server.Application.Features.Posts.Queries.GetFollowingPosts;
using buduns_server.Application.Features.Posts.Queries.GetMyPosts;
using buduns_server.Application.Features.Posts.Queries.GetPostsByUserId;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class PostController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public PostController(IMediator mediator)
        {
            _mediatR = mediator;
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Posts, ActionType = ActionType.Writing, Definition = "Create Post", AccessLevel = EndpointAccessLevel.Member)]
        [HttpPost]
        [Route("create")]
        public async Task<ActionResult<ApiResponse<CreatePostsCommandResponse>>> Create([FromBody] CreatePostsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Posts, ActionType = ActionType.Updating, Definition = "Update Post", AccessLevel = EndpointAccessLevel.Member)]
        [HttpPut]
        [Route("update")]
        public async Task<ActionResult<ApiResponse<UpdatePostsCommandResponse>>> Update([FromBody] UpdatePostsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Posts, ActionType = ActionType.Deleting, Definition = "Delete Post", AccessLevel = EndpointAccessLevel.Member)]
        [HttpDelete]
        [Route("delete")]
        public async Task<ActionResult<ApiResponse<DeletePostsCommandResponse>>> Delete([FromBody] DeletePostsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [HttpGet]
        [Route("getAll")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PostDto>>>> GetAll([FromQuery] GetAllPostsQuery request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [HttpGet]
        [Route("getById/{id}")]
        public async Task<ActionResult<ApiResponse<PostDto>>> GetById(int id)
        {
            var response = await _mediatR.Send(new GetPostByIdQuery(id));
            return Success(response);
        }

        [HttpGet]
        [Route("tag/{tagId:int}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PostDto>>>> GetByTagId(int tagId, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var response = await _mediatR.Send(new GetAllPostsByTagIdQuery { TagId = tagId, Page = page, Size = size });
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Posts, ActionType = ActionType.Reading, Definition = "Get My Posts", AccessLevel = EndpointAccessLevel.Member)]
        [HttpGet]
        [Route("me")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PostDto>>>> GetMyPosts([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var response = await _mediatR.Send(new GetMyPostsQuery { Page = page, Size = size });
            return Success(response);
        }

        [HttpGet]
        [Route("user/{userId:int}")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PostDto>>>> GetByUserId(int userId, [FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var response = await _mediatR.Send(new GetPostsByUserIdQuery { UserId = userId, Page = page, Size = size });
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Posts, ActionType = ActionType.Reading, Definition = "Get Following Posts", AccessLevel = EndpointAccessLevel.Member)]
        [HttpGet]
        [Route("following")]
        public async Task<ActionResult<ApiResponse<PagedResponse<PostDto>>>> GetFollowingPosts([FromQuery] int page = 1, [FromQuery] int size = 20)
        {
            var response = await _mediatR.Send(new GetFollowingPostsQuery { Page = page, Size = size });
            return Success(response);
        }

        [HttpGet]
        [Route("daily-top50")]
        public async Task<ActionResult<ApiResponse<List<TopPostDto>>>> GetDailyTop50()
        {
            var response = await _mediatR.Send(new GetDailyTopPostsQuery());
            return Success(response);
        }
    }
}
