using System.Security.Claims;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace buduns_server.UnitTests.Handlers;

/// <summary>
/// Handler testlerinin ortak kurulumu. Repository'ler artik dogrudan enjekte
/// edildigi icin her test yalnizca kullandigi repository'yi sahteliyor;
/// IUnitOfWork tek uyeli kaldi.
/// </summary>
internal static class HandlerTestContext
{
    public static IUnitOfWork CreateUnitOfWork() => Substitute.For<IUnitOfWork>();

    /// <summary>
    /// Verilen kullanicilari IUserRepository uzerinden bulunabilir yapar.
    /// Kaydedilmeyen bir id icin repository null doner; "kullanici yok" hali
    /// icin ayrica kurulum gerekmiyor.
    /// </summary>
    public static void RegisterUsers(IUserRepository userRepository, params User[] users)
    {
        foreach (var user in users)
        {
            userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        }
    }

    public static IHttpContextAccessor CreateHttpContextAccessor(int? viewerUserId)
    {
        if (viewerUserId == null)
        {
            return new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        }

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, viewerUserId.Value.ToString()) }, "TestAuthentication");
        return new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }

    public static User CreateUser(int id, string userName = "kullanici", Domain.Enums.UserStatus status = Domain.Enums.UserStatus.Active) => new()
    {
        Id = id,
        UserName = userName,
        FullName = userName.ToUpperInvariant(),
        Email = $"{userName}@test.com",
        EmailConfirmed = true,
        Status = status
    };
}
