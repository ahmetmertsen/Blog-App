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

namespace blogapp_server.Application.Features.Followers.Commands.Create
{
    public class CreateFollowersCommandHandler : IRequestHandler<CreateFollowersCommand, CreateFollowersCommandResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public CreateFollowersCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CreateFollowersCommandResponse> Handle(CreateFollowersCommand request, CancellationToken cancellationToken)
        {
            if (request.UserId == request.FollowingId)
            {
                throw new BadRequestException("Kullanıcı kendisini takip edemez!");
            }
            var isExits = await _unitOfWork.FollowerRepository.IsFollowExistsAsync(request.UserId, request.FollowingId);
            if (isExits == true)
            {
                throw new BadRequestException("Bu kullanıcı zaten takip ediliyor.");
            }

            var follow = new Follower
            {
                FollowerId = request.UserId,
                FollowingId = request.FollowingId,
                CreatedAt = DateTime.UtcNow,
                isActive = true,
                isDeleted = false
            };


            await _unitOfWork.FollowerRepository.AddAsync(follow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CreateFollowersCommandResponse(Succeeded: true, Message: "Kullanıcı takip edildi.");
        }
    }
}
