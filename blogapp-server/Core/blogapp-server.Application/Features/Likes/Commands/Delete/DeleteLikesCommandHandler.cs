using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Likes.Commands.Delete
{
    public class DeleteLikesCommandHandler : IRequestHandler<DeleteLikesCommand, DeleteLikesCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteLikesCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DeleteLikesCommandResponse> Handle(DeleteLikesCommand request, CancellationToken cancellationToken)
        {
            var like = await _unitOfWork.LikeRepository.GetByIdAsync(request.Id);
            if (like == null)
            {
                throw new NotFoundException("Like bulunamadı!");
            }
            if (like.UserId != request.UserId)
            {
                throw new UnauthorizedAccesException("Bu like silme yetkiniz yok.");
            }

            await _unitOfWork.LikeRepository.DeleteAsync(request.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteLikesCommandResponse(true, "Like başarıyla silinmiştir");
        }
    }
}
