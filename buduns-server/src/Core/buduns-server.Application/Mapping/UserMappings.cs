using buduns_server.Application.Dtos.User;
using buduns_server.Application.Features.Auth.Register;
using buduns_server.Domain.Entities.Identity;

namespace buduns_server.Application.Mapping
{
    public static class UserMappings
    {
        public static RegisterUserRequestDto ToRequestDto(this RegisterUserCommand command) => new()
        {
            UserName = command.UserName,
            FullName = command.FullName,
            Email = command.Email,
            Password = command.Password
        };

        // Password burada tasinmaz; parola UserManager.CreateAsync'e ayrica verilir.
        public static User ToEntity(this RegisterUserRequestDto dto) => new()
        {
            UserName = dto.UserName,
            FullName = dto.FullName,
            Email = dto.Email
        };
    }
}
