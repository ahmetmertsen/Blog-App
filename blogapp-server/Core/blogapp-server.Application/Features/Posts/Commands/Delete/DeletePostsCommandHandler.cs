using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Posts.Commands.Delete
{
    public class DeletePostsCommandHandler : IRequestHandler<DeletePostsCommand,DeletePostsCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public DeletePostsCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DeletePostsCommandResponse> Handle(DeletePostsCommand request, CancellationToken cancellationToken)
        {
            var post = await _unitOfWork.PostRepository.GetByIdAsync(request.Id);
            if (post == null)
            {
                //Exception yazılacak
            }
            await _unitOfWork.PostRepository.DeleteAsync(request.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DeletePostsCommandResponse(true, "Post başarıyla silinmiştir.");
        }
    }
}
