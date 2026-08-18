using buduns_server.Application.Common.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Roles.Commands.Update
{
    public class UpdateRoleCommand : IRequest<UpdateRoleCommandResponse>, ITransactionalRequest 
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
