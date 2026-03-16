using AutoMapper;
using blogapp_server.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Commands.Delete
{
    public class DeleteUsersCommandHandler : IRequestHandler<DeleteUsersCommand,DeleteUsersCommandResponse>
    {
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public DeleteUsersCommandHandler(UserManager<User> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<DeleteUsersCommandResponse> Handle(DeleteUsersCommand request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            if (user == null)
            {
                //Exception yazılacak
            }
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded == true)
            {
                return new DeleteUsersCommandResponse(true, "Kullanıcı başarıyla silinmiştir");
            } else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception(errors);
            }
        }
    }
}
