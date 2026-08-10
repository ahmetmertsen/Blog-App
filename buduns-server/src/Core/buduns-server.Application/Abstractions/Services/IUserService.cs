using buduns_server.Application.Dtos;
using buduns_server.Application.Dtos.User;
using buduns_server.Domain.Enums;

namespace buduns_server.Application.Abstractions.Services
{
    public interface IUserService
    {
        Task<RegisterUserResponseDto> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken);
        Task<UpdateUserPasswordResponse> UpdatePasswordAsync(UpdateUserPasswordRequest request, CancellationToken cancellationToken);
        Task<UpdateUserMailVerifyResponse> UpdateUserMailVerify(UpdateUserMailVerifyRequest request, CancellationToken cancellationToken);
        Task<UpdateUserProfileResponse> UpdateUserProfile(UpdateUserProfileRequest request);
        Task<UpdateUserEmailResponse> UpdateUserEmailAsync(UpdateUserEmailRequest request, CancellationToken cancellationToken);
        Task<(List<AdminUserDto> Items, int TotalCount)> GetPagedUsersAsync(int page, int size, string? search, UserStatus? status, bool? emailConfirmed, CancellationToken cancellationToken);
        Task<UserDto> GetUserById(int userId);
        Task<UserDto> GetUserByUserName(string userName);
        Task AssignRoleToUserAsync(int actorUserId, int targetUserId, string[] roles, CancellationToken cancellationToken);
        Task<string[]> GetRolesToUserAsync(int userId);
    }
}
