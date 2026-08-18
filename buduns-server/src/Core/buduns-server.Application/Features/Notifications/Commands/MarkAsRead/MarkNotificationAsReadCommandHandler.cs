using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Notifications.Commands.MarkAsRead
{
    public class MarkNotificationAsReadCommandHandler : IRequestHandler<MarkNotificationAsReadCommand, MarkNotificationAsReadCommandResponse>
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MarkNotificationAsReadCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
        {
            _notificationRepository = notificationRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<MarkNotificationAsReadCommandResponse> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
        {
            var found = await _notificationRepository.MarkAsReadAsync(request.Id, request.UserId, cancellationToken);
            if (!found)
            {
                throw new NotFoundException("Bildirim bulunamadı.");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new MarkNotificationAsReadCommandResponse("Bildirim okundu olarak işaretlendi.");
        }
    }
}
