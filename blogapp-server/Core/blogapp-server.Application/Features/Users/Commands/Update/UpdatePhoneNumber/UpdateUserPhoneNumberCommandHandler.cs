using blogapp_server.Application.Abstractions.Services;
using blogapp_server.Application.Dtos.User;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Update.UpdatePhoneNumber
{
    public class UpdateUserPhoneNumberCommandHandler : IRequestHandler<UpdateUserPhoneNumberCommand, UpdateUserPhoneNumberCommandResponse>
    {
        private readonly IUserService _userService;

        public UpdateUserPhoneNumberCommandHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<UpdateUserPhoneNumberCommandResponse> Handle(UpdateUserPhoneNumberCommand commandRequest, CancellationToken cancellationToken)
        {
            UpdateUserPhoneNumberRequest request = new()
            {
                UserId = commandRequest.UserId,
                NewPhoneNumber = commandRequest.NewPhoneNumber,
                PhoneNumberChangeToken = commandRequest.Token
            };

            var response = await _userService.UpdateUserPhoneNumberAsync(request);
            return new UpdateUserPhoneNumberCommandResponse(Succeeded:response.Succeeded, Message:response.Message);
        }
    }
}
