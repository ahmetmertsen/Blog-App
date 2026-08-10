using buduns_server.Application.UnitOfWork;
using MediatR;

namespace buduns_server.Application.Features.Likes.Commands.Delete
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
            var deleted = await _unitOfWork.LikeRepository.DeleteByUserAndPostAsync(request.UserId, request.PostId, cancellationToken);
            var message = deleted ? "Beğeni kaldırıldı." : "Paylaşım zaten beğenilmemiş.";
            return new DeleteLikesCommandResponse(Message: message);
        }
    }
}
