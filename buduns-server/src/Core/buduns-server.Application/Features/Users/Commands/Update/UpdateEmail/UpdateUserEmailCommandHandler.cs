using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Dtos.User;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Commands.Update.UpdateEmail
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
                OldEmailVerificationCode = commandRequest.OldEmailVerificationCode,
                NewEmailVerificationCode = commandRequest.NewEmailVerificationCode,
                NewEmail = commandRequest.NewEmail,
            };

            var response =  await _userService.UpdateUserEmailAsync(request, cancellationToken);

            UpdateUserEmailCommandResponse commandResponse = new(Message: response.Message);
            return commandResponse;
        }
    }
}
