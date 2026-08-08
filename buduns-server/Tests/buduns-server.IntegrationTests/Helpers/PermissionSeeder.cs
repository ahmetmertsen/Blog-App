using buduns_server.Application.Abstractions.Services.Configurations;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Helpers;

/// <summary>
/// RolePermissionFilter, Admin disindaki her rol icin endpoint bazli yetki
/// kaydi ariyor. Kayit yoksa [AuthorizeDefinition] tasiyan her uc 403 doner.
/// Normal kullanici akislarini test edebilmek icin bu kayitlar her testte
/// yeniden olusturulmali (Respawn tablolari bosaltiyor).
/// </summary>
public static class PermissionSeeder
{
    /// <summary>
    /// Uygulamada tanimli tum [AuthorizeDefinition] uclarini verilen rollere
    /// acar. AuthorizationEndpointService yerine dogrudan DbContext kullanilir:
    /// servis her cagrida menuleri yansimayla tarayacagi icin cok yavas kalirdi.
    /// </summary>
    public static async Task GrantAllEndpointsAsync(IServiceProvider services, params string[] roleNames)
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var applicationService = services.GetRequiredService<IApplicationService>();
        var roles = await context.Roles.Where(role => role.Name != null && roleNames.Contains(role.Name)).ToListAsync();
        if (roles.Count != roleNames.Length)
        {
            throw new InvalidOperationException($"Beklenen roller bulunamadi. Istenen: {string.Join(", ", roleNames)}");
        }

        foreach (var menuDefinition in applicationService.GetAuthorizeDefinitionEndpoints(typeof(WebAPI.Program)))
        {
            var menu = await context.Menus.FirstOrDefaultAsync(item => item.Name == menuDefinition.Name);
            if (menu == null)
            {
                menu = new Menu { Name = menuDefinition.Name, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false };
                context.Menus.Add(menu);
            }

            foreach (var action in menuDefinition.Actions)
            {
                var endpoint = new Endpoint
                {
                    Code = action.Code,
                    ActionType = action.ActionType,
                    HttpType = action.HttpType,
                    Definition = action.Definition,
                    Menu = menu,
                    CreatedAt = DateTime.UtcNow,
                    isActive = true,
                    isDeleted = false
                };

                foreach (var role in roles)
                {
                    endpoint.Roles.Add(role);
                }

                context.Endpoints.Add(endpoint);
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Tek bir yetki kodunu belirtilen rollere acar.</summary>
    public static async Task<Endpoint> GrantEndpointAsync(IServiceProvider services, string menuName, string code, params string[] roleNames)
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var applicationService = services.GetRequiredService<IApplicationService>();
        var action = applicationService.GetAuthorizeDefinitionEndpoints(typeof(WebAPI.Program))
            .FirstOrDefault(menu => menu.Name == menuName)?
            .Actions.FirstOrDefault(item => item.Code == code)
            ?? throw new InvalidOperationException($"'{menuName}' menusunde '{code}' kodlu uc bulunamadi.");

        var menuEntity = await context.Menus.FirstOrDefaultAsync(item => item.Name == menuName)
            ?? new Menu { Name = menuName, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false };

        var endpoint = new Endpoint
        {
            Code = action.Code,
            ActionType = action.ActionType,
            HttpType = action.HttpType,
            Definition = action.Definition,
            Menu = menuEntity,
            CreatedAt = DateTime.UtcNow,
            isActive = true,
            isDeleted = false
        };

        var roles = await context.Roles.Where(role => role.Name != null && roleNames.Contains(role.Name)).ToListAsync();
        foreach (var role in roles)
        {
            endpoint.Roles.Add(role);
        }

        context.Endpoints.Add(endpoint);
        await context.SaveChangesAsync();
        return endpoint;
    }

    public static async Task<List<string>> GetRoleNamesForEndpointAsync(IServiceProvider services, string code)
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var endpoint = await context.Endpoints.Include(item => item.Roles).FirstOrDefaultAsync(item => item.Code == code);
        return endpoint?.Roles.Select(role => role.Name).OfType<string>().OrderBy(name => name).ToList() ?? new List<string>();
    }
}
