using AutoMapper;
using blogapp_server.Application.Common.Interfaces;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Comments.Commands.Create
{
    public class CreateCommentsCommandHandler : IRequestHandler<CreateCommentsCommand, CreateCommentsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateCommentsCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CreateCommentsCommandResponse> Handle(CreateCommentsCommand request, CancellationToken cancellationToken)
        {
            var comment = _mapper.Map<Comment>(request);
            comment.CreatedAt = DateTime.UtcNow;
            comment.isActive = true;
            comment.isDeleted = false;

            #region Notification
            var post = await _unitOfWork.PostRepository.GetByIdAsync(request.PostId);
            if (post == null)
            {
                throw new NotFoundException("Yorum atılırken bir hata oluştu.");
            }
            Notification notification = new()
            {
                Type = Domain.Enums.NotificationType.POST_COMMENTED,
                Message = $"{post.Title} başlıklı gönderiniz yorum aldı.",
                UserId = post.UserId,
                CreatedAt = DateTime.UtcNow,
                isActive = true,
                isDeleted = false,
            };
            await _unitOfWork.NotificationRepository.AddAsync(notification);
            #endregion

            await _unitOfWork.CommentRepository.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CreateCommentsCommandResponse(Succeeded: true, Message:"Yorum başarıyla eklendi.");
        }
    }
}
