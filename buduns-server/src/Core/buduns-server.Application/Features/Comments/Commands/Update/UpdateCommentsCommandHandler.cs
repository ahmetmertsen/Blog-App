using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Comments.Commands.Update
{
    public class UpdateCommentsCommandHandler : IRequestHandler<UpdateCommentsCommand, UpdateCommentsCommandResponse>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCommentsCommandHandler(ICommentRepository commentRepository, IUnitOfWork unitOfWork)
        {
            _commentRepository = commentRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<UpdateCommentsCommandResponse> Handle(UpdateCommentsCommand request, CancellationToken cancellationToken)
        {
            var comment = await _commentRepository.GetForMutationAsync(request.Id, cancellationToken);
            if (comment == null)
            {
                throw new NotFoundException("Yorum bulunamadı.");
            }

            if (comment.UserId != request.UserId)
            {
                throw new ForbiddenException("Bu yorumu güncelleme yetkiniz yok.");
            }

            if (comment.Status != CommentStatus.Published || !comment.isActive || comment.isDeleted)
            {
                throw new BadRequestException("Yayınlanmamış bir yorum güncellenemez.");
            }

            if (comment.Post.Status != PostStatus.Published || !comment.Post.isPublished || !comment.Post.isActive || comment.Post.isDeleted)
            {
                throw new BadRequestException("Görünür olmayan bir paylaşımdaki yorum güncellenemez.");
            }

            comment.Content = request.Content.Trim();
            comment.UpdateAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var commentDto = await _commentRepository.GetDtoByIdAsync(comment.Id, cancellationToken) ?? throw new NotFoundException("Güncellenen yorum bulunamadı.");
            return new UpdateCommentsCommandResponse("Yorum başarıyla güncellendi.", commentDto);
        }
    }
}
