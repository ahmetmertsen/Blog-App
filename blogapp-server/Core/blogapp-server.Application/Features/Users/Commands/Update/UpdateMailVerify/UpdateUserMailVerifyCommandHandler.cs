using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.User;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdateMailVerify
{
    public class UpdateUserMailVerifyCommandHandler : IRequestHandler<UpdateUserMailVerifyCommand, UpdateUserMailVerifyCommandResponse>
    {
        private readonly IUserService _userService;

        public UpdateUserMailVerifyCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<UpdateUserMailVerifyCommandResponse> Handle(UpdateUserMailVerifyCommand commandRequest, CancellationToken cancellationToken)
        {
            UpdateUserMailVerifyRequest request = new()
            {
                UserId = commandRequest.UserId,
                EmailConfirmToken = commandRequest.EmailConfirmToken,
            };

            var response = await _userService.UpdateUserMailVerify(request);

            UpdateUserMailVerifyCommandResponse commandResponse = new()
            {
                Succeeded = response.Succeeded,
                Message = response.Message
            };
            return commandResponse;
        }
    }
}
