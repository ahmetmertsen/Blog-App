using AutoMapper;
using blogapp_server.Application.Dtos;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Notifications.Queries.GetAllByUserId
{
    public class GetAllNotificationsByUserIdRequestHandler : IRequestHandler<GetAllNotificationsByUserIdRequest, List<NotificationDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAllNotificationsByUserIdRequestHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<List<NotificationDto>> Handle(GetAllNotificationsByUserIdRequest request, CancellationToken cancellationToken)
        {
            var notifications = await _unitOfWork.NotificationRepository.GetAllNotificationsByUserIdAsync(request.UserId);
            var response = _mapper.Map<List<NotificationDto>>(notifications);
            return response;
        }
    }
}
