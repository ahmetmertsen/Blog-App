using blogapp_server.Application.Features.Bookmarks.Commands.Create;
using blogapp_server.Application.Features.Bookmarks.Commands.Delete;
using blogapp_server.Application.Features.Bookmarks.Queries.GetById;
using blogapp_server.Application.Features.Bookmarks.Queries.GetUserId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace blogapp_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookmarkController : ControllerBase
    {
        private readonly IMediator _mediatR;

        public BookmarkController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [Authorize]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateBookmarksCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteBookmarksCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [HttpGet]
        [Route("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _mediatR.Send(new GetBookmarkByIdRequest(id));
            return Ok(response);
        }

        [Authorize]
        [HttpGet]
        [Route("getByUserId/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var response = await _mediatR.Send(new GetBookmarksByUserIdRequest { UserId = userId });
            return Ok(response);
        }

        

    }
}
