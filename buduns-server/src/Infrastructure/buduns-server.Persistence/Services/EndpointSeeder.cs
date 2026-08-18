using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Abstractions.Services.Configurations;
using buduns_server.Application.Common.Consts;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Menu adi hem katalog DTO'sunda hem varlikta geciyor; DTO tarafi acikca
// adlandirilarak varlik adi bu dosyada sade birakiliyor.
using ActionDefinition = buduns_server.Application.Dtos.Configurations.Action;
using EndpointSeedResult = buduns_server.Application.Dtos.Configurations.EndpointSeedResult;

namespace buduns_server.Persistence.Services
{
    /// <summary>
    /// Yetki katalogunu her acilista kodla esitler. Repository yerine dogrudan
    /// DbContext kullanilir: ~50 endpoint icin kod basina sorgu atmak yerine
    /// tum katalog iki sorguda bellege alinip karsilastirilir.
    /// </summary>
    public class EndpointSeeder : IEndpointSeeder
    {
        private readonly BudunsDbContext _context;
        private readonly IApplicationService _applicationService;
        private readonly RoleManager<Role> _roleManager;
        private readonly IEndpointPermissionService _endpointPermissionService;
        private readonly ILogger<EndpointSeeder> _logger;

        public EndpointSeeder(
            BudunsDbContext context,
            IApplicationService applicationService,
            RoleManager<Role> roleManager,
            IEndpointPermissionService endpointPermissionService,
            ILogger<EndpointSeeder> logger)
        {
            _context = context;
            _applicationService = applicationService;
            _roleManager = roleManager;
            _endpointPermissionService = endpointPermissionService;
            _logger = logger;
        }

        public async Task<EndpointSeedResult> SeedAsync(Type assemblyType, CancellationToken cancellationToken)
        {
            try
            {
                return await SynchronizeAsync(assemblyType, cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                // Iki instance ayni anda kalkarsa ikisi de ayni kodu eklemeye
                // calisir ve UX_Endpoints_Code ikincisini durdurur. Ikinci
                // denemede kayitlar yerinde bulunur; bu bir hata degil, yaristir.
                _logger.LogWarning(exception, "Endpoint katalogu yazilirken cakisma olustu, bir kez daha deneniyor.");

                _context.ChangeTracker.Clear();
                return await SynchronizeAsync(assemblyType, cancellationToken);
            }
        }

        private async Task<EndpointSeedResult> SynchronizeAsync(Type assemblyType, CancellationToken cancellationToken)
        {
            var definitions = _applicationService.GetAuthorizeDefinitionEndpoints(assemblyType);

            var menus = await _context.Menus.ToDictionaryAsync(menu => menu.Name, cancellationToken);
            var endpoints = await _context.Endpoints
                .Include(endpoint => endpoint.Menu)
                .Include(endpoint => endpoint.Roles)
                .ToDictionaryAsync(endpoint => endpoint.Code, cancellationToken);
            var roles = await _roleManager.Roles.Where(role => role.Name != null).ToListAsync(cancellationToken);

            var createdMenuCount = 0;
            var createdEndpointCount = 0;
            var updatedEndpointCount = 0;
            var createdCodes = new List<string>();
            var definedCodes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var menuDefinition in definitions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!menus.TryGetValue(menuDefinition.Name, out var menu))
                {
                    menu = new Menu { Name = menuDefinition.Name, CreatedAt = DateTime.UtcNow, isActive = true, isDeleted = false };
                    _context.Menus.Add(menu);
                    menus[menuDefinition.Name] = menu;
                    createdMenuCount++;
                }

                foreach (var action in menuDefinition.Actions)
                {
                    definedCodes.Add(action.Code);

                    if (!endpoints.TryGetValue(action.Code, out var endpoint))
                    {
                        _context.Endpoints.Add(CreateEndpoint(action, menu, roles));
                        createdEndpointCount++;
                        createdCodes.Add(action.Code);
                        continue;
                    }

                    if (UpdateDescriptiveFields(endpoint, action, menu))
                    {
                        updatedEndpointCount++;
                    }
                }
            }

            var orphanCodes = endpoints.Keys
                .Where(code => !definedCodes.Contains(code))
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToList();

            await _context.SaveTranslatedAsync(cancellationToken);

            // Yeni kod, onbellekte "kaydi yok" olarak durabilir; o girdi
            // dusurulmezse uc TTL boyunca varsayilan rollerle degil, eski
            // negatif sonucla degerlendirilir.
            foreach (var code in createdCodes)
            {
                await _endpointPermissionService.InvalidateAsync(code, cancellationToken);
            }

            return new EndpointSeedResult
            {
                CreatedMenuCount = createdMenuCount,
                CreatedEndpointCount = createdEndpointCount,
                UpdatedEndpointCount = updatedEndpointCount,
                OrphanCodes = orphanCodes
            };
        }

        private static Endpoint CreateEndpoint(ActionDefinition action, Menu menu, IReadOnlyCollection<Role> roles)
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

            var defaultRoles = RoleConstants.GetDefaultRoles(action.AccessLevel);
            foreach (var role in roles.Where(role => defaultRoles.Contains(role.Name!)))
            {
                endpoint.Roles.Add(role);
            }

            return endpoint;
        }

        /// <summary>
        /// Var olan kaydin yalnizca tanimlayici alanlari tazelenir; rol kumesine
        /// dokunulmaz. Yonetim ucundan yapilmis bir atamayi her acilista geri
        /// almak, yetki yonetimini kullanilamaz hale getirirdi.
        /// </summary>
        private static bool UpdateDescriptiveFields(Endpoint endpoint, ActionDefinition action, Menu menu)
        {
            var changed = false;

            if (endpoint.ActionType != action.ActionType)
            {
                endpoint.ActionType = action.ActionType;
                changed = true;
            }

            if (endpoint.HttpType != action.HttpType)
            {
                endpoint.HttpType = action.HttpType;
                changed = true;
            }

            if (endpoint.Definition != action.Definition)
            {
                endpoint.Definition = action.Definition;
                changed = true;
            }

            if (!ReferenceEquals(endpoint.Menu, menu) && endpoint.Menu?.Name != menu.Name)
            {
                endpoint.Menu = menu;
                changed = true;
            }

            if (changed)
            {
                endpoint.UpdateAt = DateTime.UtcNow;
            }

            return changed;
        }
    }
}
