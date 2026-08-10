using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Notifications.Commands.MarkAllAsRead
{
    public class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, MarkAllNotificationsAsReadCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public MarkAllNotificationsAsReadCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<MarkAllNotificationsAsReadCommandResponse> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
        {
            var updatedCount = await _unitOfWork.NotificationRepository.MarkAllAsReadAsync(request.UserId, cancellationToken);
            if (updatedCount > 0)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return new MarkAllNotificationsAsReadCommandResponse("Bildirimler okundu olarak işaretlendi.", updatedCount);
        }
    }
}
