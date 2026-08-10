using buduns_server.Application.Abstractions.Services.Configurations;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos.Configurations;
using buduns_server.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Infrastructure.Services.Configurations
{
    public class ApplicationService : IApplicationService
    {
        public List<Menu> GetAuthorizeDefinitionEndpoints(Type type)
        {
            Assembly assembly = Assembly.GetAssembly(type)
                ?? throw new InvalidOperationException($"'{type.FullName}' turunun assembly'si bulunamadi.");

            var controllers = assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(ControllerBase)));

            List<Menu> menus = new();

            foreach (var controller in controllers)
            {
                var actions = controller.GetMethods().Where(m => m.IsDefined(typeof(AuthorizeDefinitionAttribute)));
                foreach (var action in actions)
                {
                    var attributes = action.GetCustomAttributes(true);

                    // IsDefined ile filtrelendigi icin normalde her zaman bulunur;
                    // yine de bulunamazsa bu action atlanir.
                    if (attributes.FirstOrDefault(a => a.GetType() == typeof(AuthorizeDefinitionAttribute)) is not AuthorizeDefinitionAttribute authorizeDefinitonAttribute)
                    {
                        continue;
                    }

                    var menu = menus.FirstOrDefault(m => m.Name == authorizeDefinitonAttribute.Menu);
                    if (menu == null)
                    {
                        menu = new() { Name = authorizeDefinitonAttribute.Menu };
                        menus.Add(menu);
                    }

                    var httpAttribute = attributes.FirstOrDefault(a => a.GetType().IsAssignableTo(typeof(HttpMethodAttribute))) as HttpMethodAttribute;
                    var httpType = httpAttribute?.HttpMethods.First() ?? HttpMethods.Get;
                    var actionType = Enum.GetName(typeof(ActionType), authorizeDefinitonAttribute.ActionType) ?? string.Empty;
                    var definition = authorizeDefinitonAttribute.Definition ?? string.Empty;

                    menu.Actions.Add(new Application.Dtos.Configurations.Action
                    {
                        ActionType = actionType,
                        Definition = definition,
                        HttpType = httpType,
                        Code = $"{httpType}.{actionType}.{definition.Replace(" ", "")}",
                        AccessLevel = authorizeDefinitonAttribute.AccessLevel,
                        DefaultRoles = RoleConstants.GetDefaultRoles(authorizeDefinitonAttribute.AccessLevel)
                    });
                }
            }

            return menus;
        }
    }
}
