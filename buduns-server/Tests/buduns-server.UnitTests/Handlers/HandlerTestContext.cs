using System.Security.Claims;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace buduns_server.UnitTests.Handlers;

/// <summary>
/// Handler testlerinin ortak kurulumu. IUnitOfWork on iki repository tasidigi
/// ve UserManager'in dokuz bagimliligi oldugu icin her testte elle kurmak
/// gurultuye yol aciyordu.
/// </summary>
internal static class HandlerTestContext
{
    public static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.BookmarkRepository.Returns(Substitute.For<IBookmarkRepository>());
        unitOfWork.CommentRepository.Returns(Substitute.For<ICommentRepository>());
        unitOfWork.FollowerRepository.Returns(Substitute.For<IFollowerRepository>());
        unitOfWork.LikeRepository.Returns(Substitute.For<ILikeRepository>());
        unitOfWork.NotificationRepository.Returns(Substitute.For<INotificationRepository>());
        unitOfWork.PostRepository.Returns(Substitute.For<IPostRepository>());
        unitOfWork.TagRepository.Returns(Substitute.For<ITagRepository>());
        unitOfWork.UtilityRepository.Returns(Substitute.For<IUtilityRepository>());
        unitOfWork.ReportRepository.Returns(Substitute.For<IReportRepository>());
        unitOfWork.ModerationActionRepository.Returns(Substitute.For<IModerationActionRepository>());
        unitOfWork.EndpointRepository.Returns(Substitute.For<IEndpointRepository>());
        unitOfWork.MenuRepository.Returns(Substitute.For<IMenuRepository>());
        return unitOfWork;
    }

    public static UserManager<User> CreateUserManager(params User[] users)
    {
        var manager = Substitute.For<UserManager<User>>(
            Substitute.For<IUserStore<User>>(),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Substitute.For<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);

        foreach (var user in users)
        {
            manager.FindByIdAsync(user.Id.ToString()).Returns(user);
        }

        return manager;
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
