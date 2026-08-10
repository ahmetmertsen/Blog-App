using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Abstractions.Services.Configurations;
using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Persistence.Services
{
    public class AuthorizationEndpointService : IAuthorizationEndpointService
    {
        private readonly IApplicationService _applicationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RoleManager<Role> _roleManager;

        public AuthorizationEndpointService(IApplicationService applicationService, IUnitOfWork unitOfWork, RoleManager<Role> roleManager)
        {
            _applicationService = applicationService;
            _unitOfWork = unitOfWork;
            _roleManager = roleManager;
        }

        public async Task AssignRoleEndpointAsync(string[] roles, string menu, string code, Type type)
        {
            CancellationToken cancellationToken = new();

            Menu? _menu = await _unitOfWork.MenuRepository.GetMenuByNameAsync(menu);
            if (_menu == null)
            {
                _menu = new()
                {
                    Name = menu,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.MenuRepository.AddAsync(_menu);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }



            var endpoint = await _unitOfWork.EndpointRepository.GetEndpointWithMenuByCodeAsync(code, menu);
            if (endpoint == null)
            {
                var action = _applicationService.GetAuthorizeDefinitionEndpoints(type).FirstOrDefault(m => m.Name == menu)?
                    .Actions.FirstOrDefault(e => e.Code == code);
                if (action == null)
                {
                    throw new NotFoundException($"'{menu}' menusunde '{code}' kodlu endpoint tanimi bulunamadi.");
                }

                endpoint = new()
                {
                    Code = code,
                    ActionType = action.ActionType,
                    HttpType = action.HttpType,
                    Definition = action.Definition,
                    Menu = _menu
                };

                await _unitOfWork.EndpointRepository.AddAsync(endpoint);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // Atama, mevcut rol kumesini degistirir; uzerine eklemez.
            endpoint.Roles.Clear();

            var appRoles = await _roleManager.Roles.Where(r => roles.Contains(r.Name)).ToListAsync();

            foreach (var role in appRoles)
            {
                endpoint.Roles.Add(role);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

        }

        public async Task<List<string>> GetRolesToEndpoint(string code, string menu)
        {

            Endpoint? endpoint = await _unitOfWork.EndpointRepository.GetRolesToEndpointWithMenu(code, menu);
            if (endpoint == null)
            {
                throw new NotFoundException("Endpoint bulunamadı.");
            }

            // Role.Name (IdentityRole) nullable; adsiz roller listeye alinmiyor.
            return endpoint.Roles.Select(r => r.Name).OfType<string>().ToList();
        }
    }
}
