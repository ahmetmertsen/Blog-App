using blogapp_server.Application.Dtos.Auth;
using blogapp_server.Application.Features.Auth.ChangeEmail;
using blogapp_server.Application.Features.Auth.ForgotPassword;
using blogapp_server.Application.Features.Auth.Login;
using blogapp_server.Application.Features.Auth.MailVerify;
using blogapp_server.Application.Features.Auth.RefreshTokenLogin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace blogapp_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediatR;
        public AuthController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [HttpPost]
        [Route("refreshTokenLogin")]
        public async Task<IActionResult> RefreshTokenLogin([FromBody] RefreshTokenLoginCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [HttpPost]
        [Route("forgotPassword")]
        public async Task<IActionResult> ForgotPasswordReset([FromBody] ForgotPasswordCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpPost]
        [Route("mailVerify")]
        public async Task<IActionResult> MailVerify([FromBody] MailVerifyCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpPost]
        [Route("emailChange")]
        public async Task<IActionResult> EmailChange([FromBody] ChangeEmailCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }
    }
}
