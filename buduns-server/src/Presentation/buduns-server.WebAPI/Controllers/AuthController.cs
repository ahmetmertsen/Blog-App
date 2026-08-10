using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Features.Auth.ChangeEmail;
using buduns_server.Application.Features.Auth.ForgotPassword;
using buduns_server.Application.Features.Auth.GetSessions;
using buduns_server.Application.Features.Auth.Login;
using buduns_server.Application.Features.Auth.Logout;
using buduns_server.Application.Features.Auth.LogoutAll;
using buduns_server.Application.Features.Auth.MailVerify;
using buduns_server.Application.Features.Auth.RefreshTokenLogin;
using buduns_server.Application.Features.Auth.RevokeSession;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : ApiControllerBase
    {
        private readonly IMediator _mediatR;
        public AuthController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [HttpPost]
        [Route("login")]
        public async Task<ActionResult<ApiResponse<LoginUserCommandResponse>>> Login([FromBody] LoginUserCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [HttpPost]
        [Route("refreshTokenLogin")]
        public async Task<ActionResult<ApiResponse<RefreshTokenLoginCommandResponse>>> RefreshTokenLogin([FromBody] RefreshTokenLoginCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [HttpPost]
        [Route("forgotPassword")]
        public async Task<ActionResult<ApiResponse<ForgotPasswordCommandResponse>>> ForgotPasswordReset([FromBody] ForgotPasswordCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Auth, ActionType = ActionType.Writing, Definition = "Send Mail Verify")]
        [HttpPost]
        [Route("mailVerify")]
        public async Task<ActionResult<ApiResponse<MailVerifyCommandResponse>>> MailVerify()
        {
            var response = await _mediatR.Send(new MailVerifyCommand());
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Auth, ActionType = ActionType.Updating, Definition = "Change Email")]
        [HttpPost]
        [Route("emailChange")]
        public async Task<ActionResult<ApiResponse<ChangeEmailCommandResponse>>> EmailChange([FromBody] ChangeEmailCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [HttpPost]
        [Route("logout")]
        public async Task<ActionResult<ApiResponse<LogoutCommandResponse>>> Logout()
        {
            var response = await _mediatR.Send(new LogoutCommand());
            return Success(response);
        }

        [Authorize]
        [HttpPost]
        [Route("logoutAll")]
        public async Task<ActionResult<ApiResponse<LogoutAllCommandResponse>>> LogoutAll()
        {
            var response = await _mediatR.Send(new LogoutAllCommand());
            return Success(response);
        }

        [Authorize]
        [HttpGet]
        [Route("sessions")]
        public async Task<ActionResult<ApiResponse<GetAuthSessionsQueryResponse>>> GetSessions()
        {
            var response = await _mediatR.Send(new GetAuthSessionsQuery());
            return Success(response);
        }

        [Authorize]
        [HttpDelete]
        [Route("sessions/{sessionId:guid}")]
        public async Task<ActionResult<ApiResponse<RevokeSessionCommandResponse>>> RevokeSession(Guid sessionId)
        {
            var response = await _mediatR.Send(new RevokeSessionCommand { SessionId = sessionId });
            return Success(response);
        }
    }
}
