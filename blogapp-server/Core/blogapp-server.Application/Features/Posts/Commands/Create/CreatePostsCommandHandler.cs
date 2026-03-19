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
            var tagIds = request.TagIds?
                .Distinct()
                .ToList() ?? new List<int>();

            var tags = await _unitOfWork.TagRepository.GetByIdsAsync(tagIds);
            var foundTagIds = tags.Select(t => t.Id).ToHashSet();
            var missingTagIds = tagIds.Where(id => !foundTagIds.Contains(id)).ToList();

            if (missingTagIds.Any())
            {
                throw new BadRequestException($"Geçersiz tag id(ler): {string.Join(", ", missingTagIds)}");
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
