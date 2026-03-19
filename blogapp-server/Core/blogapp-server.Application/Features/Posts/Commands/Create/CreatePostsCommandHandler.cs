using AutoMapper;
using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Commands.Create
{
    public class CreatePostsCommandHandler : IRequestHandler<CreatePostsCommand, CreatePostsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreatePostsCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<CreatePostsCommandResponse> Handle(CreatePostsCommand request, CancellationToken cancellationToken)
        {
            #region Tag Ekleme
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
            #endregion


            var post = _mapper.Map<Post>(request);
            post.UserId = request.UserId;
            post.CreatedAt = DateTime.UtcNow;
            post.isActive = true;
            post.isDeleted = false;
            post.Tags = tags;

 
            await _unitOfWork.PostRepository.AddAsync(post);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CreatePostsCommandResponse(true, "Post başarıyla eklenmiştir.");
        }
    }
}
