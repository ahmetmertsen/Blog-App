using blogapp_server.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Users.Queries.GetById
{
    public record GetUserByIdRequest(int Id) : IRequest<UserDto>
    {
    }
}
