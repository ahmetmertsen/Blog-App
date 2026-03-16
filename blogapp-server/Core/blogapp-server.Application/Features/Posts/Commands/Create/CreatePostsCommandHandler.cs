using AutoMapper;
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
            var post = _mapper.Map<Post>(request);
            post.UserId = request.UserId;
            post.CreatedAt = DateTime.UtcNow;
            post.isActive = true;
            post.isDeleted = false;

            await _unitOfWork.PostRepository.AddAsync(post);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CreatePostsCommandResponse(true, "Post başarıyla eklenmiştir.");
        }
    }
}
