using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace buduns_server.Application.Features.Comments.Commands.Delete
{
    public class DeleteCommentsCommandHandler : IRequestHandler<DeleteCommentsCommand, DeleteCommentsCommandResponse>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteCommentsCommandHandler> _logger;

        public DeleteCommentsCommandHandler(ICommentRepository commentRepository, IUnitOfWork unitOfWork, ILogger<DeleteCommentsCommandHandler> logger)
        {
            _commentRepository = commentRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DeleteCommentsCommandResponse> Handle(DeleteCommentsCommand request, CancellationToken cancellationToken)
        {
            var comment = await _commentRepository.GetForMutationAsync(request.Id, cancellationToken);
            if (comment == null)
            {
                throw new NotFoundException("Yorum bulunamadı.");
            }

            if (comment.UserId != request.UserId)
            {
                throw new ForbiddenException("Bu yorumu silme yetkiniz yok.");
            }

            if (comment.Status == CommentStatus.DeletedByOwner)
            {
                return new DeleteCommentsCommandResponse("Yorum daha önce silinmiş.");
            }

            if (comment.Status != CommentStatus.Published)
            {
                throw new BadRequestException("Moderasyon işlemi uygulanmış bir yorum silinemez.");
            }

            comment.Status = CommentStatus.DeletedByOwner;
            comment.isActive = false;
            comment.isDeleted = true;
            comment.UpdateAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Comment deleted by owner. CommentId: {CommentId}, PostId: {PostId}, UserId: {UserId}", comment.Id, comment.PostId, request.UserId);
            return new DeleteCommentsCommandResponse("Yorum başarıyla silindi.");
        }
    }
}
