using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Bookmarks.Commands.Create;
using buduns_server.Application.Features.Bookmarks.Commands.Delete;
using buduns_server.Application.Features.Bookmarks.Queries.GetBookmarks;
using buduns_server.Application.Features.Bookmarks.Queries.GetStatus;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class BookmarkController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public BookmarkController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Bookmarks, ActionType = ActionType.Writing, Definition = "Create Bookmark", AccessLevel = EndpointAccessLevel.Member)]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CreateBookmarksCommandResponse>>> Create([FromBody] CreateBookmarksCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Bookmarks, ActionType = ActionType.Deleting, Definition = "Delete Bookmark", AccessLevel = EndpointAccessLevel.Member)]
        [HttpDelete("{postId:int}")]
        public async Task<ActionResult<ApiResponse<DeleteBookmarksCommandResponse>>> Delete(int postId)
        {
            var response = await _mediatR.Send(new DeleteBookmarksCommand { PostId = postId });
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Bookmarks, ActionType = ActionType.Reading, Definition = "Get Bookmarks", AccessLevel = EndpointAccessLevel.Member)]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<BookmarkDto>>>> GetBookmarks([FromQuery] GetBookmarksQuery request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Bookmarks, ActionType = ActionType.Reading, Definition = "Get Bookmark Status", AccessLevel = EndpointAccessLevel.Member)]
        [HttpGet("status/{postId:int}")]
        public async Task<ActionResult<ApiResponse<GetBookmarkStatusQueryResponse>>> GetStatus(int postId)
        {
            var response = await _mediatR.Send(new GetBookmarkStatusQuery { PostId = postId });
            return Success(response);
        }
    }
}
