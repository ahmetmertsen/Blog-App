using blogapp_server.Application.Features.Posts.Commands.Create;
using blogapp_server.Application.Features.Posts.Commands.Delete;
using blogapp_server.Application.Features.Posts.Commands.Update;
using blogapp_server.Application.Features.Posts.Queries.GetAll;
using blogapp_server.Application.Features.Posts.Queries.GetById;
using blogapp_server.Application.Features.Users.Commands.Delete;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace blogapp_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PostController : ControllerBase
    {
        private readonly IMediator _mediatR;

        public PostController(IMediator mediator)
        {
            _mediatR = mediator;
        }

        [Authorize]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreatePostsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> Update([FromBody] UpdatePostsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> Delete([FromBody] DeletePostsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [HttpGet]
        [Route("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _mediatR.Send(new GetAllPostsRequest());
            return Ok(response);
        }

        [HttpGet]
        [Route("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _mediatR.Send(new GetPostByIdRequest(id));
            return Ok(response);
        }
    }
}
