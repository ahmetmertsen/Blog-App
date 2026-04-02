using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.User;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdateEmail
{
    public class UpdateUserEmailCommandHandler : IRequestHandler<UpdateUserEmailCommand, UpdateUserEmailCommandResponse>
    {
        private readonly IUserService _userService;

        public UpdateUserEmailCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<UpdateUserEmailCommandResponse> Handle(UpdateUserEmailCommand commandRequest, CancellationToken cancellationToken)
        {
            UpdateUserEmailRequest request = new()
            {
                UserId = commandRequest.UserId,
                ChangeEmailToken = commandRequest.ChangeEmailToken,
                NewEmail = commandRequest.NewEmail,
            };

            var response =  await _userService.UpdateUserEmailAsync(request);

            UpdateUserEmailCommandResponse commandResponse = new(Succeeded: response.Succeeded, Message: response.Message);
            return commandResponse;
        }
    }
}
