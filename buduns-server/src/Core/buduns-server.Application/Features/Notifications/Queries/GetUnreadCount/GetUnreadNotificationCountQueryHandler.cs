using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Notifications.Queries.GetUnreadCount
{
    public class GetUnreadNotificationCountQueryHandler : IRequestHandler<GetUnreadNotificationCountQuery, GetUnreadNotificationCountQueryResponse>
    {
        private readonly INotificationRepository _notificationRepository;

        public GetUnreadNotificationCountQueryHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<GetUnreadNotificationCountQueryResponse> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
        {
            var unreadCount = await _notificationRepository.GetUnreadCountAsync(request.UserId, cancellationToken);
            return new GetUnreadNotificationCountQueryResponse(unreadCount);
        }
    }
}
