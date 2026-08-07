using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.AuthorizationEndpoint.Queries.GetRolesToEndpoint
{
    public class GetRolesToEndpointQuery : IRequest<GetRolesToEndpointQueryResponse>
    {
        public string Code { get; set; } = string.Empty;
        public string Menu { get; set; } = string.Empty;
    }
}
