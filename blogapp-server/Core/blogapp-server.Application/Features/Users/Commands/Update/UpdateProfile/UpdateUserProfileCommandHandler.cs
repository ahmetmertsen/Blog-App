using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.User;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdateProfile
{
    public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, UpdateUserProfileCommandResponse>
    {
        private readonly IUserService _userService;

        public UpdateUserProfileCommandHandler(IUserService userService )
        {
            _userService = userService;
        }

        public async Task<UpdateUserProfileCommandResponse> Handle(UpdateUserProfileCommand commandRequest, CancellationToken cancellationToken)
        {
            UpdateUserProfileRequest request = new()
            {
                UserId = commandRequest.UserId,
                FullName = commandRequest.FullName,
                Bio = commandRequest.Bio,
                ImageUrl = commandRequest.ImageUrl,
            };
            UpdateUserProfileResponse response =  await _userService.UpdateUserProfile(request);

            return new UpdateUserProfileCommandResponse(Succeeded: response.Succeeded, Message: response.Message);
        }
    }
}
