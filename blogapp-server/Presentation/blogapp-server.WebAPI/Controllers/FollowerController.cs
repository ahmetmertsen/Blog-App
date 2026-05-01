using blogapp_server.Application.Features.Followers.Commands.Create;
using blogapp_server.Application.Features.Followers.Commands.Delete;
using blogapp_server.Application.Features.Followers.Queries.GetAllByUserId;
using blogapp_server.Application.Features.Followers.Queries.GetById;
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
        [Route("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _mediatR.Send(new GetFollowerByIdRequest(id));
            return Ok(response);
        }

        
        [HttpGet]
        [Route("getAllFollowersByUserId/{userId}")]
        public async Task<IActionResult> GetAllFollowersByUserId(int userId)
        {
            var response = await _mediatR.Send(new GetAllFollowersByUserIdRequest(userId));
            return Ok(response);
        }

        [HttpGet]
        [Route("getAllFollowingsByUserId/{userId}")]
        public async Task<IActionResult> GetAllFollowinsByUserId(int userId)
        {
            var response = await _mediatR.Send(new GetAllFollowingsByUserIdRequest(userId));
            return Ok(response);
        }
        
    }
}
