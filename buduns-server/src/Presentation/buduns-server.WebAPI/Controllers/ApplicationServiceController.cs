using buduns_server.Application.Abstractions.Services.Configurations;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos.Configurations;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ApplicationServiceController : ApiControllerBase
    {
        private readonly IApplicationService _applicationService;

        public ApplicationServiceController(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        [AuthorizeDefinition(Menu = "Application Services", ActionType = ActionType.Reading, Definition = "Get Authorize Definition Endpoints")]
        [HttpGet]
        public ActionResult<ApiResponse<List<Menu>>> GetAuthorizeDefinitionEndpoints()
        {
            var datas = _applicationService.GetAuthorizeDefinitionEndpoints(typeof(Program));
            return Success(datas);
        }
    }
}
