using buduns_server.Application.Dtos;
using buduns_server.Application.Exceptions;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using MediatR;

namespace buduns_server.Application.Features.Followers.Queries.GetAllByUserId
{
    public class GetAllFollowingsByUserIdQueryHandler : IRequestHandler<GetAllFollowingsByUserIdQuery, PagedResponse<FollowerDto>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public GetAllFollowingsByUserIdQueryHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedResponse<FollowerDto>> Handle(GetAllFollowingsByUserIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null || user.Status == UserStatus.Banned)
            {
                throw new NotFoundException("Kullanıcı bulunamadı.");
            }

            var result = await _unitOfWork.FollowerRepository.GetPagedFollowingsByUserIdAsync(request.UserId, request.Page, request.Size, cancellationToken);
            return new PagedResponse<FollowerDto> { Items = result.Items, Page = request.Page, Size = request.Size, TotalCount = result.TotalCount };
        }
    }
}
