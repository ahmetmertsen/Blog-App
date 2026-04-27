using blogapp_server.Application.Features.Auth.Register;
using blogapp_server.Application.Features.Users.Commands.Update.UpdateEmail;
using blogapp_server.Application.Features.Users.Commands.Update.UpdateMailVerify;
using blogapp_server.Application.Features.Users.Commands.Update.UpdatePassword;
using blogapp_server.Application.Features.Users.Commands.Update.UpdateProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace blogapp_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMediator _mediatR;

        public UserController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [HttpPost]
        [Route("updatePassword")]
        public async Task<IActionResult> UpdateUserPassword([FromBody] UpdateUserPasswordCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpPost]
        [Route("updateMailVerify")]
        public async Task<IActionResult> UpdateUserMailVerify([FromBody] UpdateUserMailVerifyCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpPost]
        [Route("updateUserProfile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserProfileCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpPost]
        [Route("updateUserEmail")]
        public async Task<IActionResult> UpdateUserEmail([FromBody] UpdateUserEmailCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }
    }
}
