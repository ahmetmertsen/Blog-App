using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Mapping;
using buduns_server.Application.Repositories;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.Features.Followers.Queries.GetById
{
    public class GetFollowerByIdQueryHandler : IRequestHandler<GetFollowerByIdQuery, FollowerDto>
    {
        private readonly IFollowerRepository _followerRepository;

        public GetFollowerByIdQueryHandler(IFollowerRepository followerRepository)
        {
            _followerRepository = followerRepository;
        }

        public async Task<FollowerDto> Handle(GetFollowerByIdQuery request, CancellationToken cancellationToken)
        {
            var follower = await _followerRepository.GetByIdAsync(request.Id);
            if (follower == null)
            {
                throw new NotFoundException("Takipçi bulunamadı!");
            }
            var response = follower.ToDto();
            return response;
        }
    }
}
