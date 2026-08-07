using buduns_server.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Auth.RefreshTokenLogin
{
    public class RefreshTokenLoginCommandResponse
    {
        public required Token Token { get; set; }
    }
}
