using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.User;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdatePassword
{
    public class UpdateUserPasswordCommandHandler : IRequestHandler<UpdateUserPasswordCommand, UpdateUserPasswordCommandResponse>
    {
        private readonly IUserService _userService;

        public UpdateUserPasswordCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<UpdateUserPasswordCommandResponse> Handle(UpdateUserPasswordCommand commandRequest, CancellationToken cancellationToken)
        {
            UpdateUserPasswordRequest request = new()
            {
                UserId = commandRequest.UserId,
                ResetToken = commandRequest.ResetToken,
                newPassword = commandRequest.newPassword,
                newPasswordConfirmed = commandRequest.newPasswordConfirmed,
            };

            var response = await _userService.UpdatePasswordAsync(request);

            UpdateUserPasswordCommandResponse commandResponse = new()
            {
                Succeeded = response.Succedded,
                Message = response.Message
            };
            return commandResponse;
        }
    }
}
