using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Roles.Commands.Delete
{
    public class DeleteRoleCommand : IRequest<DeleteRoleCommandResponse>, ITransactionalRequest
    {
        public int Id { get; set; }
    }
}
