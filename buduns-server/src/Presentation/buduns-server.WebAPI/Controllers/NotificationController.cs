using buduns_server.Application.Common.Consts;
using buduns_server.Application.Common.CustomAttrributes;
using buduns_server.Application.Dtos;
using buduns_server.Application.Features.Notifications.Commands.Delete;
using buduns_server.Application.Features.Notifications.Commands.MarkAllAsRead;
using buduns_server.Application.Features.Notifications.Commands.MarkAsRead;
using buduns_server.Application.Features.Notifications.Queries.GetAllByUserId;
using buduns_server.Application.Features.Notifications.Queries.GetUnreadCount;
using buduns_server.Domain.Enums;
using buduns_server.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace buduns_server.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ApiControllerBase
    {
        private readonly IMediator _mediatR;

        public NotificationController(IMediator mediatR)
        {
            _mediatR = mediatR;
        }


        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Notification, ActionType = ActionType.Deleting, Definition = "Delete Notification")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<DeleteNotificationCommandResponse>>> Delete(int id)
        {
            var response = await _mediatR.Send(new DeleteNotificationCommand { Id = id });
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Notification, ActionType = ActionType.Reading, Definition = "Get My Notifications")]
        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResponse<NotificationDto>>>> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] bool onlyUnread = false)
        {
            var response = await _mediatR.Send(new GetAllNotificationsByUserIdQuery { Page = page, Size = size, OnlyUnread = onlyUnread });
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Notification, ActionType = ActionType.Reading, Definition = "Get Unread Notification Count")]
        [HttpGet("unread-count")]
        public async Task<ActionResult<ApiResponse<GetUnreadNotificationCountQueryResponse>>> GetUnreadCount()
        {
            var response = await _mediatR.Send(new GetUnreadNotificationCountQuery());
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Notification, ActionType = ActionType.Updating, Definition = "Mark Notification As Read")]
        [HttpPatch("{id:int}/read")]
        public async Task<ActionResult<ApiResponse<MarkNotificationAsReadCommandResponse>>> MarkAsRead(int id)
        {
            var response = await _mediatR.Send(new MarkNotificationAsReadCommand { Id = id });
            return Success(response);
        }

        [AuthorizeDefinition( Menu = AuthorizeDefinitionConstants.Notification, ActionType = ActionType.Updating, Definition = "Mark All Notifications As Read")]
        [HttpPatch("read-all")]
        public async Task<ActionResult<ApiResponse<MarkAllNotificationsAsReadCommandResponse>>> MarkAllAsRead()
        {
            var response = await _mediatR.Send(new MarkAllNotificationsAsReadCommand());
            return Success(response);
        }
    }
}
