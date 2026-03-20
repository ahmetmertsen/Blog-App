using blogapp_server.Application.Features.Followers.Commands.Create;
using blogapp_server.Application.Features.Followers.Commands.Delete;
using blogapp_server.Application.Features.Followers.Queries.GetAll;
using blogapp_server.Application.Features.Followers.Queries.GetAllByUsername;
using blogapp_server.Application.Features.Followers.Queries.GetById;
using blogapp_server.Application.Features.Likes.Commands.Create;
using blogapp_server.Application.Features.Likes.Commands.Delete;
using blogapp_server.Application.Features.Likes.Queries.GetAll;
using blogapp_server.Application.Features.Likes.Queries.GetAllByUsername;
using blogapp_server.Application.Features.Likes.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace blogapp_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FollowerController : ControllerBase
    {
        private readonly IMediator _mediatR;

        public FollowerController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [Authorize]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateFollowersCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteFollowersCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [HttpGet]
        [Route("getAll")]
        public async Task<IActionResult> GetAll()
        {
            var response = await _mediatR.Send(new GetAllFollowersRequest());
            return Ok(response);
        }

        [HttpGet]
        [Route("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _mediatR.Send(new GetFollowerByIdRequest(id));
            return Ok(response);
        }

        [HttpGet]
        [Route("getAllFollowersByUserName/{userName}")]
        public async Task<IActionResult> GetAllFollowersByUserName(string userName)
        {
            var response = await _mediatR.Send(new GetAllFollowersByUsernameRequest(userName));
            return Ok(response);
        }

        [HttpGet]
        [Route("getAllFollowingsByUserName/{userName}")]
        public async Task<IActionResult> GetAllFollowinsByUserName(string userName)
        {
            var response = await _mediatR.Send(new GetAllFollowingsByUsernameRequest(userName));
            return Ok(response);
        }
    }
}
