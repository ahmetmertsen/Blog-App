using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Auth.RefreshTokenLogin
{
    public class RefreshTokenLoginCommand : IRequest<RefreshTokenLoginCommandResponse>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}
