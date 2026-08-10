using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos;
using buduns_server.Application.Dtos.User;
using buduns_server.Application.Features.Auth.Register;
using buduns_server.Application.Features.Users.Commands.AssignRoleToUser;
using buduns_server.Application.Features.Users.Commands.Update.UpdateEmail;
using buduns_server.Application.Features.Users.Commands.Update.UpdateMailVerify;
using buduns_server.Application.Features.Users.Commands.Update.UpdatePassword;
using buduns_server.Application.Features.Users.Commands.Update.UpdateProfile;
using buduns_server.Application.Features.Users.Queries.GetAll;
using buduns_server.Application.Features.Users.Queries.GetById;
using buduns_server.Application.Features.Users.Queries.GetByUsername;
using buduns_server.Application.Features.Users.Queries.GetRolesToUser;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    public class UserController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public UserController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }

        [HttpPost]
        [Route("register")]
        public async Task<ActionResult<ApiResponse<RegisterUserCommandResponse>>> Register([FromBody] RegisterUserCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [HttpPost]
        [Route("updatePassword")]
        public async Task<ActionResult<ApiResponse<UpdateUserPasswordCommandResponse>>> UpdateUserPassword([FromBody] UpdateUserPasswordCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Users, ActionType = ActionType.Updating, Definition = "Update User Mail Verify", AccessLevel = EndpointAccessLevel.Member)]
        [HttpPost]
        [Route("updateMailVerify")]
        public async Task<ActionResult<ApiResponse<UpdateUserMailVerifyCommandResponse>>> UpdateUserMailVerify([FromBody] UpdateUserMailVerifyCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Users, ActionType = ActionType.Updating, Definition = "Update User Profile", AccessLevel = EndpointAccessLevel.Member)]
        [HttpPost]
        [Route("updateUserProfile")]
        public async Task<ActionResult<ApiResponse<UpdateUserProfileCommandResponse>>> UpdateUserProfile([FromBody] UpdateUserProfileCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize]
        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Users, ActionType = ActionType.Updating, Definition = "Update User Email", AccessLevel = EndpointAccessLevel.Member)]
        [HttpPost]
        [Route("updateUserEmail")]
        public async Task<ActionResult<ApiResponse<UpdateUserEmailCommandResponse>>> UpdateUserEmail([FromBody] UpdateUserEmailCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [Authorize(Roles = "Admin")]
        [AuthorizeDefinition(Menu = AuthorizeDefinitionConstants.Users, ActionType = ActionType.Reading, Definition = "GetAll Users")]
        [HttpGet]
        [Route("getAllUsers")]
        public async Task<ActionResult<ApiResponse<PagedResponse<AdminUserDto>>>> GetAllUsers([FromQuery] GetAllUsersQuery request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }

        [HttpGet]
        [Route("getUserById/{userId}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int userId)
        {
            var response = await _mediatR.Send(new GetUserByIdQuery() { UserId = userId });
            return Success(response);
        }

        [HttpGet]
        [Route("getUserByUsername/{userName}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByUsername(string userName)
        {
            var response = await _mediatR.Send(new GetUserByUsernameQuery() { UserName = userName });
            return Success(response);
        }

        [Authorize(Roles = "Admin")]
        [AuthorizeDefinition(Menu = AuthorizeDefinitionConstants.Users, ActionType = ActionType.Reading, Definition = "Get Roles To User")]
        [HttpGet]
        [Route("getRolesToUser/{userId}")]
        public async Task<ActionResult<ApiResponse<GetRolesToUserQueryResponse>>> GetRolesToUser(int userId)
        {
            var response = await _mediatR.Send(new GetRolesToUserQuery() { UserId = userId });
            return Success(response);
        }


        [Authorize(Roles = "Admin")]
        [AuthorizeDefinition(Menu = AuthorizeDefinitionConstants.Users, ActionType = ActionType.Writing, Definition = "Assign Role To User")]
        [HttpPost]
        [Route("assignRoleToUser")]
        public async Task<ActionResult<ApiResponse<AssignRoleToUserCommandResponse>>> AssignRoleToUser([FromBody] AssignRoleToUserCommand request)
        {
            var response = await _mediatR.Send(request);
            return Success(response);
        }
    }
}
