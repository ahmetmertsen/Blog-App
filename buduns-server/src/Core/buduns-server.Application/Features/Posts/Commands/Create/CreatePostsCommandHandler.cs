using buduns_server.Application.Exceptions;
using buduns_server.Application.Mapping;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Posts.Commands.Create
{
    public class CreatePostsCommandHandler : IRequestHandler<CreatePostsCommand, CreatePostsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CreatePostsCommandHandler> _logger;

        public CreatePostsCommandHandler(IUnitOfWork unitOfWork, ILogger<CreatePostsCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<CreatePostsCommandResponse> Handle(CreatePostsCommand request, CancellationToken cancellationToken)
        {
            #region Tag Ekleme
            var tagIds = request.TagIds?
                .Distinct()
                .ToList() ?? new List<int>();

            var tags = await _unitOfWork.TagRepository.GetByIdsAsync(tagIds, cancellationToken);
            var foundTagIds = tags.Select(t => t.Id).ToHashSet();
            var missingTagIds = tagIds.Where(id => !foundTagIds.Contains(id)).ToList();

            if (missingTagIds.Any())
            {
                throw new BadRequestException($"Geçersiz tag id(ler): {string.Join(", ", missingTagIds)}");
            }
            #endregion


            var post = request.ToEntity();
            post.UserId = request.UserId;
            post.CreatedAt = DateTime.UtcNow;
            post.UpdateAt = post.CreatedAt;
            post.isActive = true;
            post.isDeleted = false;
            post.isPublished = true;
            post.Status = Domain.Enums.PostStatus.Published;
            post.Tags = tags;

 
            await _unitOfWork.PostRepository.AddAsync(post);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Post created. PostId: {PostId}, UserId: {UserId}, TagCount: {TagCount}", post.Id, request.UserId, tagIds.Count);

            return new CreatePostsCommandResponse("Post başarıyla eklenmiştir.");
        }
    }
}
