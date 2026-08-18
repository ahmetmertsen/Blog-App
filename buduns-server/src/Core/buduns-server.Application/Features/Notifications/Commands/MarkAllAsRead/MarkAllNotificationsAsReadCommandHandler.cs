using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, MarkAllNotificationsAsReadCommandResponse>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MarkAllNotificationsAsReadCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<MarkAllNotificationsAsReadCommandResponse> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
        {
            var updatedCount = await _notificationRepository.MarkAllAsReadAsync(request.UserId, cancellationToken);
            if (updatedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new MarkAllNotificationsAsReadCommandResponse("Bildirimler okundu olarak işaretlendi.", updatedCount);
        }
    }
}
