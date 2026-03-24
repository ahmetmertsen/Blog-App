using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Notifications.Commands.Delete
{
    public class DeleteNotificationsCommandHandler : IRequestHandler<DeleteNotificationCommand, DeleteNotificationsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteNotificationsCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DeleteNotificationsCommandResponse> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _unitOfWork.NotificationRepository.GetByIdAsync(request.Id);
            if (notification == null)
            {
                throw new NotFoundException("Bildirim bulunamadı!");
            }
            if (notification.UserId != request.UserId)
            {
                throw new UnauthorizedAccesException("Bu bildirimi silme yetkiniz yok.");
            }

            await _unitOfWork.NotificationRepository.DeleteAsync(request.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DeleteNotificationsCommandResponse(true, "Bildirim başarıyla silinmiştir.");
        }
    }
}
