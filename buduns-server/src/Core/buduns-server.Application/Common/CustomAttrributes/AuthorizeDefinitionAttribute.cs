using buduns_server.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Common.CustomAttrributes
{
    public class AuthorizeDefinitionAttribute : Attribute
    {
        public required string Menu { get; set; }
        public required string Definition { get; set; }
        public ActionType ActionType { get; set; }

        /// <summary>
        /// Endpoint ilk kez seed edilirken hangi rollere acilacagi. Belirtilmezse
        /// hicbir role acilmaz; yeni bir uc yanlislikla acik dogamaz.
        /// </summary>
        public EndpointAccessLevel AccessLevel { get; set; } = EndpointAccessLevel.AdminOnly;
    }
}
