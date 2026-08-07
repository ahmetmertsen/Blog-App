using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Notifications.Commands.Delete
{
    public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, DeleteNotificationCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteNotificationCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DeleteNotificationCommandResponse> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _unitOfWork.NotificationRepository.SoftDeleteByIdAndUserAsync(request.Id, request.UserId, cancellationToken);
            if (!deleted)
            {
                throw new NotFoundException("Bildirim bulunamadı.");
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DeleteNotificationCommandResponse(true, "Bildirim başarıyla silinmiştir.");
        }
    }
}
