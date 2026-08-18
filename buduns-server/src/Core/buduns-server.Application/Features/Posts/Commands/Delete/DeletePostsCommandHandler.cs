using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Posts.Commands.Delete
{
    public class DeletePostsCommandHandler : IRequestHandler<DeletePostsCommand,DeletePostsCommandResponse>
    {
        private readonly IPostRepository _postRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeletePostsCommandHandler> _logger;
        
        public DeletePostsCommandHandler(IPostRepository postRepository, IUnitOfWork unitOfWork, ILogger<DeletePostsCommandHandler> logger)
        {
            _postRepository = postRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DeletePostsCommandResponse> Handle(DeletePostsCommand request, CancellationToken cancellationToken)
        {
            var post = await _postRepository.GetByIdWithTagsAsync(request.Id);
            if (post == null)
            {
                throw new NotFoundException("Post bulunamadı!");
            }
            if (post.UserId != request.UserId)
            {
                throw new UnauthorizedAccesException("Bu postu silme yetkiniz yok.");
            }

            post.Status = PostStatus.DeletedByOwner;
            post.isPublished = false;
            post.isActive = false;
            post.isDeleted = true;
            post.UpdateAt = DateTime.UtcNow;
            _postRepository.Update(post);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post deleted. PostId: {PostId}, UserId: {UserId}", request.Id, request.UserId);

            return new DeletePostsCommandResponse("Post başarıyla silinmiştir.");
        }
    }
}
