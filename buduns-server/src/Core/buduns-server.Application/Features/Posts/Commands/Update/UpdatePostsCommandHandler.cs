using buduns_server.Application.Exceptions;
using buduns_server.Application.Mapping;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Posts.Commands.Update
{
    public class UpdatePostsCommandHandler : IRequestHandler<UpdatePostsCommand, UpdatePostsCommandResponse>
    {
        private readonly IPostRepository _postRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdatePostsCommandHandler(IPostRepository postRepository, ITagRepository tagRepository, IUnitOfWork unitOfWork)
        {
            _postRepository = postRepository;
            _tagRepository = tagRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<UpdatePostsCommandResponse> Handle(UpdatePostsCommand request, CancellationToken cancellationToken)
        {

            var post = await _postRepository.GetByIdWithTagsAsync(request.Id);
            if (post == null)
            {
                throw new NotFoundException("Post bulunamadı!");
            }
            if (post.UserId != request.UserId)
            {
                throw new UnauthorizedAccesException("Bu postu güncelleme yetkiniz yok.");
            }
            #region Tag Güncelleme
            var tagIds = request.TagIds?
                .Distinct()
                .ToList() ?? new List<int>();
            var tags = await _tagRepository.GetByIdsAsync(tagIds, cancellationToken);
            var foundTagIds = tags.Select(t => t.Id).ToHashSet();
            var missingTagIds = tagIds.Where(id => !foundTagIds.Contains(id)).ToList();

            if (missingTagIds.Any())
            {
                throw new BadRequestException($"Geçersiz tag id(ler): {string.Join(", ", missingTagIds)}");
            }

            post.Tags.Clear();
            foreach (var tag in tags)
            {
                post.Tags.Add(tag);
            }
            #endregion

            request.ApplyTo(post);
            post.UpdateAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new UpdatePostsCommandResponse("Post başarıyla güncellenmiştir");
        }
    }
}
