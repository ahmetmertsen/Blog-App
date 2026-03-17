using blogapp_server.Application.Features.Bookmarks.Commands.Create;
using blogapp_server.Application.Features.Bookmarks.Commands.Delete;
using blogapp_server.Application.Features.Bookmarks.Queries.GetAll;
using blogapp_server.Application.Features.Bookmarks.Queries.GetAllByUsername;
using blogapp_server.Application.Features.Bookmarks.Queries.GetById;
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
        [Route("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _mediatR.Send(new GetAllBookmarksRequest());
            return Ok(response);
        }

        [HttpGet]
        [Route("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _mediatR.Send(new GetBookmarkByIdRequest(id));
            return Ok(response);
        }

        [HttpGet]
        [Route("getAllByUserName/{userName}")]
        public async Task<IActionResult> GetByUserName(string userName)
        {
            var response = await _mediatR.Send(new GetAllBookmarksByUsernameRequest(userName));
            return Ok(response);
        }
    }
}
