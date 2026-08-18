using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Followers.Queries.GetAllByUserId
{
    public class GetAllFollowingsByUserIdQueryHandler : IRequestHandler<GetAllFollowingsByUserIdQuery, PagedResponse<FollowerDto>>
    {
        private readonly IFollowerRepository _followerRepository;
        private readonly IUserRepository _userRepository;

        public GetAllFollowingsByUserIdQueryHandler(IFollowerRepository followerRepository, IUserRepository userRepository)
        {
            _followerRepository = followerRepository;
            _userRepository = userRepository;
        }

        public async Task<PagedResponse<FollowerDto>> Handle(GetAllFollowingsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null || user.Status == UserStatus.Banned)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            var result = await _followerRepository.GetPagedFollowingsByUserIdAsync(request.UserId, request.Page, request.Size, cancellationToken);
            return new PagedResponse<FollowerDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
