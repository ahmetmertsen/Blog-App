using buduns_server.Application.Abstractions.Services;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Helpers;

/// <summary>
/// Yetki kayitlarini artik testler degil, uygulamanin acilistaki
/// <see cref="IEndpointSeeder"/>'i olusturuyor (BudunsWebApplicationFactory.
/// ResetStateAsync). Burada kalan yardimcilar yalnizca **var olan** bir kaydi
/// degistirir: bir ucu kapatmak, baska bir role devretmek ya da kaydi
/// tamamen silip filtrenin varsayilana dusme davranisini gorebilmek icin.
/// </summary>
public static class PermissionSeeder
{
    /// <summary>
    /// Bir yetki kodunun rol kumesini verilen listeyle degistirir. Rol
    /// verilmezse uc herkese kapanir. Onbellek de dusurulur; uretimde bunu
    /// AuthorizationEndpointService yapiyor, dogrudan veritabanina yazan bu
    /// yardimci de ayni sorumlulugu tasimali.
    /// </summary>
    public static async Task SetEndpointRolesAsync(IServiceProvider services, string code, params string[] roleNames)
    {
        var context = services.GetRequiredService<BudunsDbContext>();

        var endpoint = await context.Endpoints.Include(item => item.Roles).FirstOrDefaultAsync(item => item.Code == code)
            ?? throw new InvalidOperationException($"'{code}' kodlu endpoint kaydi yok. Seeder calismamis olabilir.");

        endpoint.Roles.Clear();

        var roles = await context.Roles.Where(role => role.Name != null && roleNames.Contains(role.Name)).ToListAsync();
        if (roles.Count != roleNames.Length)
        {
            throw new InvalidOperationException($"Beklenen roller bulunamadi. Istenen: {string.Join(", ", roleNames)}");
        }

        foreach (var role in roles)
        {
            endpoint.Roles.Add(role);
        }

        await context.SaveChangesAsync();
        await services.GetRequiredService<IEndpointPermissionService>().InvalidateAsync(code);
    }

    /// <summary>
    /// Kaydi tumuyle siler. "Rolleri bosalt" ile ayni sey degil: kayit yoksa
    /// filtre kodda bildirilen varsayilan seviyeye duser.
    /// </summary>
    public static async Task DeleteEndpointAsync(IServiceProvider services, string code)
    {
        var context = services.GetRequiredService<BudunsDbContext>();

        var endpoint = await context.Endpoints.Include(item => item.Roles).FirstOrDefaultAsync(item => item.Code == code)
            ?? throw new InvalidOperationException($"'{code}' kodlu endpoint kaydi yok. Seeder calismamis olabilir.");

        endpoint.Roles.Clear();
        context.Endpoints.Remove(endpoint);

        await context.SaveChangesAsync();
        await services.GetRequiredService<IEndpointPermissionService>().InvalidateAsync(code);
    }

    public static async Task<List<string>> GetRoleNamesForEndpointAsync(IServiceProvider services, string code)
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var endpoint = await context.Endpoints.Include(item => item.Roles).FirstOrDefaultAsync(item => item.Code == code);
        return endpoint?.Roles.Select(role => role.Name).OfType<string>().OrderBy(name => name).ToList() ?? new List<string>();
    }
}
