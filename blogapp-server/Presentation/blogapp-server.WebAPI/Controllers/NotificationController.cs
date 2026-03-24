using blogapp_server.Application.Features.Likes.Queries.GetById;
using blogapp_server.Application.Features.Notifications.Commands.Delete;
using blogapp_server.Application.Features.Notifications.Queries.GetAllByUserId;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace blogapp_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly IMediator _mediatR;

        public NotificationController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }


        [Authorize]
        [HttpDelete]
        [Route("delete")]
        public async Task<IActionResult> Delete([FromBody] DeleteNotificationCommand request)
        {
            var response = await _mediatR.Send(request);
            return Ok(response);
        }

        [Authorize]
        [HttpGet]
        [Route("myNotifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            var response = await _mediatR.Send(new GetAllNotificationsByUserIdRequest());
            return Ok(response);
        }
    }
}
