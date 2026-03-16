using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Common.Interfaces
{
    public interface ICurrentUserRequest
    {
        public int UserId { get; set; }
    }
}
