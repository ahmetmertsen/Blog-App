using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Likes.Commands.Delete
{
    public class DeleteLikesCommandHandler : IRequestHandler<DeleteLikesCommand, DeleteLikesCommandResponse>
    {
        private readonly ILikeRepository _likeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteLikesCommandHandler(ILikeRepository likeRepository, IUnitOfWork unitOfWork)
        {
            _likeRepository = likeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<DeleteLikesCommandResponse> Handle(DeleteLikesCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _likeRepository.DeleteByUserAndPostAsync(request.UserId, request.PostId, cancellationToken);
            if (deleted)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            var message = deleted ? "Beğeni kaldırıldı." : "Paylaşım zaten beğenilmemiş.";
            return new DeleteLikesCommandResponse(Message: message);
        }
    }
}
