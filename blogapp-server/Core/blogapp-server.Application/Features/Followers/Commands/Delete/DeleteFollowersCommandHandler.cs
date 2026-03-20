using blogapp_server.Application.Exceptions;
using blogapp_server.Application.UnitOfWork;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.Features.Followers.Commands.Delete
{
    public class DeleteFollowersCommandHandler : IRequestHandler<DeleteFollowersCommand, DeleteFollowersCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public DeleteFollowersCommandHandler(IUnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<DeleteFollowersCommandResponse> Handle(DeleteFollowersCommand request, CancellationToken cancellationToken)
        {
            var follow = await _unitOfWork.FollowerRepository.GetFollowAsync(request.UserId, request.FollowingId);
            if (follow == null)
            {
                throw new NotFoundException("Takip ilişkisi bulunamadı!");
            }

            _unitOfWork.FollowerRepository.Delete(follow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteFollowersCommandResponse(Succeeded: true, Message: "Takip bırakıldı.");
        }
    }
}
