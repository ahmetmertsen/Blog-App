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
            var menu = await GetOrAddMenuAsync(context, menuDefinition.Name);

            foreach (var action in menuDefinition.Actions)
            {
                var endpoint = await GetOrAddEndpointAsync(context, menu, action);

                foreach (var role in roles.Where(role => !endpoint.Roles.Contains(role)))
                {
                    endpoint.Roles.Add(role);
                }
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

        var menuEntity = await GetOrAddMenuAsync(context, menuName);
        var endpoint = await GetOrAddEndpointAsync(context, menuEntity, action);

        var roles = await context.Roles.Where(role => role.Name != null && roleNames.Contains(role.Name)).ToListAsync();
        foreach (var role in roles.Where(role => !endpoint.Roles.Contains(role)))
        {
            endpoint.Roles.Add(role);
        }

        await context.SaveChangesAsync();
        return endpoint;
    }

    /// <summary>
    /// Endpoint kayitlari artik acilista EndpointSeeder tarafindan olusturuluyor;
    /// bu yardimcilar var olan kaydin uzerine yazmali, ikinci bir kopya
    /// eklememeli (Endpoints.Code uzerinde benzersiz indeks var).
    /// </summary>
    private static async Task<Menu> GetOrAddMenuAsync(BudunsDbContext context, string menuName)
    {
        var menu = context.ChangeTracker.Entries<Menu>().Select(entry => entry.Entity).FirstOrDefault(item => item.Name == menuName)
            ?? await context.Menus.FirstOrDefaultAsync(item => item.Name == menuName);

        if (menu != null)
        {
            return menu;
        }

        menu = new Menu { Name = menuName, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false };
        context.Menus.Add(menu);
        return menu;
    }

    private static async Task<Endpoint> GetOrAddEndpointAsync(BudunsDbContext context, Menu menu, Application.Dtos.Configurations.Action action)
    {
        var endpoint = context.ChangeTracker.Entries<Endpoint>().Select(entry => entry.Entity).FirstOrDefault(item => item.Code == action.Code)
            ?? await context.Endpoints.Include(item => item.Roles).FirstOrDefaultAsync(item => item.Code == action.Code);

        if (endpoint != null)
        {
            return endpoint;
        }

        endpoint = new Endpoint
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

        context.Endpoints.Add(endpoint);
        return endpoint;
    }

    public static async Task<List<string>> GetRoleNamesForEndpointAsync(IServiceProvider services, string code)
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var endpoint = await context.Endpoints.Include(item => item.Roles).FirstOrDefaultAsync(item => item.Code == code);
        return endpoint?.Roles.Select(role => role.Name).OfType<string>().OrderBy(name => name).ToList() ?? new List<string>();
    }
}
