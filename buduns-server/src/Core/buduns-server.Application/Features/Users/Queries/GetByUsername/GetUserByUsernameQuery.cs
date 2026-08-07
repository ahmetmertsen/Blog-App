using buduns_server.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Users.Queries.GetByUsername
{
    public class GetUserByUsernameQuery : IRequest<UserDto>
    {
        public string UserName { get; set; } = string.Empty;
    }
}
