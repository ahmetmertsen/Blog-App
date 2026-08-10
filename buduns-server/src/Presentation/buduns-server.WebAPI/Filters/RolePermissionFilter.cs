using buduns_server.Application.Abstractions.Services;
using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;

namespace buduns_server.WebAPI.Filters
{
    public class RolePermissionFilter : IAsyncActionFilter
    {
        private readonly IUserService _userService;
        private readonly ILogger<RolePermissionFilter> _logger;

        public RolePermissionFilter(IUserService userService, ILogger<RolePermissionFilter> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
            if (descriptor == null)
            {
                await next();
                return;
            }

            var attribute = descriptor.MethodInfo.GetCustomAttribute(typeof(AuthorizeDefinitionAttribute)) as AuthorizeDefinitionAttribute;
            if (attribute == null)
            {
                await next();
                return;
            }

            if (context.HttpContext.User.IsInRole(RoleConstants.Admin))
            {
                await next();
                return;
            }

            var httpAttribute = descriptor.MethodInfo.GetCustomAttribute(typeof(HttpMethodAttribute)) as HttpMethodAttribute;

            var httpType = httpAttribute != null ? httpAttribute.HttpMethods.First() : HttpMethods.Get;

            var code = $"{httpType}.{attribute.ActionType}.{attribute.Definition.Replace(" ", "")}";

            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? context.HttpContext.User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning(
                    "Permission check failed. Reason: {Reason}, Path: {Path}, Controller: {Controller}, Action: {Action}, PermissionCode: {PermissionCode}, Menu: {Menu}, Definition: {Definition}",
                    "InvalidUserIdentifier",
                    context.HttpContext.Request.Path,
                    descriptor.ControllerName,
                    descriptor.ActionName,
                    code,
                    attribute.Menu,
                    attribute.Definition);

                context.Result = new UnauthorizedResult(); // 401: login yok
                return;
            }

            var hasRole = await _userService.HasRolePermissionToEndpointAsync(userId, code);

            if (!hasRole)
            {
                _logger.LogWarning(
                    "Permission denied. UserId: {UserId}, Path: {Path}, Controller: {Controller}, Action: {Action}, PermissionCode: {PermissionCode}, Menu: {Menu}, Definition: {Definition}",
                    userId,
                    context.HttpContext.Request.Path,
                    descriptor.ControllerName,
                    descriptor.ActionName,
                    code,
                    attribute.Menu,
                    attribute.Definition);

                context.Result = new ForbidResult(); // 403: login var ama yetki yok
                return;
            }

            await next();
        }
    }
}
