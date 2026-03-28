using AutoMapper;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Likes.Commands.Create
{
    public class CreateLikesCommandHandler : IRequestHandler<CreateLikesCommand, CreateLikesCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateLikesCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CreateLikesCommandResponse> Handle(CreateLikesCommand request, CancellationToken cancellationToken)
        {
            var like = _mapper.Map<Like>(request);
            like.isActive = true;
            like.CreatedAt = DateTime.UtcNow;
            like.isDeleted = false;

            #region Like notification
            var post = await _unitOfWork.PostRepository.GetByIdAsync(request.PostId);
            if (post == null) 
            {
                throw new NotFoundException("Bir hata oluştu.");
            }

            Notification notification = new()
            {
                Type = Domain.Enums.NotificationType.POST_LIKED,
                Message = $"{post.Title} başlıklı göndederiniz beğeni aldı.",
                UserId = post.UserId,
                CreatedAt = DateTime.UtcNow,
                isActive = true,
                isDeleted = false,
            };
            await _unitOfWork.NotificationRepository.AddAsync(notification);
            #endregion

            await _unitOfWork.LikeRepository.AddAsync(like);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CreateLikesCommandResponse(true, "Like başarıyla atılmıştır.");
        }
    }
}
