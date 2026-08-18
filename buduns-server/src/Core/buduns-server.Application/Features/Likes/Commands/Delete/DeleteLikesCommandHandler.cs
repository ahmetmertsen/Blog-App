using buduns_server.Application.Repositories;
using MediatR;

namespace buduns_server.Application.Features.Likes.Commands.Delete
{
    public class DeleteLikesCommandHandler : IRequestHandler<DeleteLikesCommand, DeleteLikesCommandResponse>
    {
        private readonly ILikeRepository _likeRepository;

        public DeleteLikesCommandHandler(ILikeRepository likeRepository)
        {
            _likeRepository = likeRepository;
        }

        public async Task<DeleteLikesCommandResponse> Handle(DeleteLikesCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _likeRepository.DeleteByUserAndPostAsync(request.UserId, request.PostId, cancellationToken);
            var message = deleted ? "Beğeni kaldırıldı." : "Paylaşım zaten beğenilmemiş.";
            return new DeleteLikesCommandResponse(Message: message);
        }
    }
}
