using buduns_server.WebAPI.Http;
using buduns_server.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [ApiController]
    public abstract class ApiControllerBase : ControllerBase
    {
        // Basari zarfinin tek kurma noktasi. Hata zarfini ApiErrorWriter kurar.
        protected ApiResponse<T> Success<T>(T data) => new()
        {
            IsSuccess = true,
            Data = data,
            TraceId = HttpContext.GetTraceId()
        };
    }
}
