using blogapp_server.Application.Features.Comments.Commands.Create;
using blogapp_server.Application.Features.Comments.Commands.Delete;
using blogapp_server.Application.Features.Comments.Commands.Update;
using blogapp_server.Application.Features.Comments.Queries.GetAll;
using blogapp_server.Application.Features.Comments.Queries.GetAllByUsername;
using blogapp_server.Application.Features.Comments.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace blogapp_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly IMediator _mediatR;

        public CommentController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [Authorize]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateCommentsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpPut]
        [Route("update")]
        public async Task<IActionResult> Update([FromBody] UpdateCommentsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteCommentsCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [HttpGet]
        [Route("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _mediatR.Send(new GetAllCommentsRequest());
            return Ok(response);
        }

        [HttpGet]
        [Route("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _mediatR.Send(new GetCommentByIdRequest(id));
            return Ok(response);
        }

        [HttpGet]
        [Route("getByUserName/{userName}")]
        public async Task<IActionResult> GetByUserName(string userName)
        {
            var response = await _mediatR.Send(new GetAllCommentsByUsernameRequest(userName));
            return Ok(response);
        }
    }
}
