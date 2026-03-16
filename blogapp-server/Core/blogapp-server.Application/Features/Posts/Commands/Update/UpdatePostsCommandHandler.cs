using AutoMapper;
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
            var post = await _unitOfWork.PostRepository.GetByIdAsync(request.Id);
            if (post == null)
            {
                //Exception yazılacak
            }
            _mapper.Map(request, post);
            post.UpdateAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new UpdatePostsCommandResponse(true, "Post başarıyla güncellenmiştir");
        }
    }
}
