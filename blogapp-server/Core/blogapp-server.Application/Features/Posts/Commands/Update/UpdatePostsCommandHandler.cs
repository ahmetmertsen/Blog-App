using AutoMapper;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Commands.Update
{
    public class UpdatePostsCommandHandler : IRequestHandler<UpdatePostsCommand, UpdatePostsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public UpdatePostsCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<UpdatePostsCommandResponse> Handle(UpdatePostsCommand request, CancellationToken cancellationToken)
        {

            var post = await _unitOfWork.PostRepository.GetByIdWithTagsAsync(request.Id);
            if (post == null)
            {
                throw new NotFoundException("Post bulunamadı!");
            }
            if (post.UserId != request.UserId)
            {
                throw new UnauthorizedAccesException("Bu postu güncelleme yetkiniz yok.");
            }
            #region Tag Güncelleme
            var normalizedTagNames = request.TagNames?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLower())
                .Distinct()
                .ToList() ?? new List<string>();
            var tags = await _unitOfWork.TagRepository.GetByNamesAsync(normalizedTagNames);
            var foundTagNames = tags
                .Select(t => t.Name.ToLower())
                .ToHashSet();

            var missingTags = normalizedTagNames
                .Where(name => !foundTagNames.Contains(name))
                .ToList();

            if (missingTags.Any())
            {
                throw new BadRequestException($"Bu tag(ler) sistemde tanımlı değil: {string.Join(", ", missingTags)}");
            }

            post.Tags.Clear();
            foreach (var tag in tags)
            {
                post.Tags.Add(tag);
            }
            #endregion

            _mapper.Map(request, post);
            post.UpdateAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new UpdatePostsCommandResponse(true, "Post başarıyla güncellenmiştir");
        }
    }
}
