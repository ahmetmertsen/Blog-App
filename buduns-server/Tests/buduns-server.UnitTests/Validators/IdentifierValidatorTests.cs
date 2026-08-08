using buduns_server.Application.Features.Bookmarks.Commands.Create;
using buduns_server.Application.Features.Bookmarks.Commands.Delete;
using buduns_server.Application.Features.Bookmarks.Queries.GetStatus;
using buduns_server.Application.Features.Comments.Commands.Delete;
using buduns_server.Application.Features.Comments.Queries.GetById;
using buduns_server.Application.Features.Followers.Commands.Create;
using buduns_server.Application.Features.Followers.Commands.Delete;
using buduns_server.Application.Features.Followers.Queries.GetById;
using buduns_server.Application.Features.Followers.Queries.GetStatus;
using buduns_server.Application.Features.Likes.Commands.Create;
using buduns_server.Application.Features.Likes.Commands.Delete;
using buduns_server.Application.Features.Likes.Queries.GetById;
using buduns_server.Application.Features.Likes.Queries.GetStatus;
using buduns_server.Application.Features.Notifications.Commands.Delete;
using buduns_server.Application.Features.Notifications.Commands.MarkAsRead;
using buduns_server.Application.Features.Posts.Commands.Delete;
using buduns_server.Application.Features.Posts.Queries.GetById;
using buduns_server.Application.Features.Report.Queries.GetById;
using buduns_server.Application.Features.Roles.Commands.Delete;
using buduns_server.Application.Features.Roles.Queries.GetById;
using buduns_server.Application.Features.Tags.Commands.Delete;
using buduns_server.Application.Features.Tags.Queries.GetById;
using buduns_server.Application.Features.Users.Queries.GetById;
using FluentValidation;
using FluentValidation.Results;

namespace buduns_server.UnitTests.Validators;

/// <summary>
/// "Id 0'dan buyuk olmalidir" kurali yirmi ustu komut/sorguda kopyalanmis
/// durumda. Kopyalarin birinde kural dusurulurse 0 veya negatif kimlik
/// dogrudan handler'a ulasir; bu testler hepsini ayni yerden dogrular.
/// </summary>
public class IdentifierValidatorTests
{
    private sealed record IdentifierCase(string Name, string PropertyName, Func<int, Task<ValidationResult>> Validate);

    private static readonly IdentifierCase[] Cases =
    {
        Id<CreateBookmarksCommand>(nameof(CreateBookmarksCommand), nameof(CreateBookmarksCommand.PostId), id => new CreateBookmarksCommand { PostId = id }, new CreateBookmarksCommandValidator()),
        Id<DeleteBookmarksCommand>(nameof(DeleteBookmarksCommand), nameof(DeleteBookmarksCommand.PostId), id => new DeleteBookmarksCommand { PostId = id }, new DeleteBookmarksCommandValidator()),
        Id<GetBookmarkStatusQuery>(nameof(GetBookmarkStatusQuery), nameof(GetBookmarkStatusQuery.PostId), id => new GetBookmarkStatusQuery { PostId = id }, new GetBookmarkStatusQueryValidator()),
        Id<DeleteCommentsCommand>(nameof(DeleteCommentsCommand), nameof(DeleteCommentsCommand.Id), id => new DeleteCommentsCommand { Id = id }, new DeleteCommentsCommandValidator()),
        Id<GetCommentByIdQuery>(nameof(GetCommentByIdQuery), nameof(GetCommentByIdQuery.Id), id => new GetCommentByIdQuery(id), new GetCommentByIdQueryValidator()),
        Id<CreateFollowersCommand>(nameof(CreateFollowersCommand), nameof(CreateFollowersCommand.FollowingId), id => new CreateFollowersCommand { FollowingId = id }, new CreateFollowersCommandValidator()),
        Id<DeleteFollowersCommand>(nameof(DeleteFollowersCommand), nameof(DeleteFollowersCommand.FollowingId), id => new DeleteFollowersCommand { FollowingId = id }, new DeleteFollowersCommandValidator()),
        Id<GetFollowerByIdQuery>(nameof(GetFollowerByIdQuery), nameof(GetFollowerByIdQuery.Id), id => new GetFollowerByIdQuery(id), new GetFollowerByIdQueryValidator()),
        Id<GetFollowerStatusQuery>(nameof(GetFollowerStatusQuery), nameof(GetFollowerStatusQuery.FollowingId), id => new GetFollowerStatusQuery { FollowingId = id }, new GetFollowerStatusQueryValidator()),
        Id<CreateLikesCommand>(nameof(CreateLikesCommand), nameof(CreateLikesCommand.PostId), id => new CreateLikesCommand { PostId = id }, new CreateLikesCommandValidator()),
        Id<DeleteLikesCommand>(nameof(DeleteLikesCommand), nameof(DeleteLikesCommand.PostId), id => new DeleteLikesCommand { PostId = id }, new DeleteLikesCommandValidator()),
        Id<GetLikeByIdQuery>(nameof(GetLikeByIdQuery), nameof(GetLikeByIdQuery.Id), id => new GetLikeByIdQuery(id), new GetLikeByIdQueryValidator()),
        Id<GetLikeStatusQuery>(nameof(GetLikeStatusQuery), nameof(GetLikeStatusQuery.PostId), id => new GetLikeStatusQuery { PostId = id }, new GetLikeStatusQueryValidator()),
        Id<DeleteNotificationCommand>(nameof(DeleteNotificationCommand), nameof(DeleteNotificationCommand.Id), id => new DeleteNotificationCommand { Id = id }, new DeleteNotificationCommandValidator()),
        Id<MarkNotificationAsReadCommand>(nameof(MarkNotificationAsReadCommand), nameof(MarkNotificationAsReadCommand.Id), id => new MarkNotificationAsReadCommand { Id = id }, new MarkNotificationAsReadCommandValidator()),
        Id<DeletePostsCommand>(nameof(DeletePostsCommand), nameof(DeletePostsCommand.Id), id => new DeletePostsCommand { Id = id }, new DeletePostsCommandValidator()),
        Id<GetPostByIdQuery>(nameof(GetPostByIdQuery), nameof(GetPostByIdQuery.Id), id => new GetPostByIdQuery(id), new GetPostByIdQueryValidator()),
        Id<GetReportByIdQuery>(nameof(GetReportByIdQuery), nameof(GetReportByIdQuery.ReportId), id => new GetReportByIdQuery { ReportId = id }, new GetReportByIdQueryValidator()),
        Id<DeleteRoleCommand>(nameof(DeleteRoleCommand), nameof(DeleteRoleCommand.Id), id => new DeleteRoleCommand { Id = id }, new DeleteRoleCommandValidator()),
        Id<GetRoleByIdQuery>(nameof(GetRoleByIdQuery), nameof(GetRoleByIdQuery.Id), id => new GetRoleByIdQuery { Id = id }, new GetRoleByIdQueryValidator()),
        Id<DeleteTagsCommand>(nameof(DeleteTagsCommand), nameof(DeleteTagsCommand.Id), id => new DeleteTagsCommand(id), new DeleteTagsCommandValidator()),
        Id<GetTagByIdQuery>(nameof(GetTagByIdQuery), nameof(GetTagByIdQuery.Id), id => new GetTagByIdQuery(id), new GetTagByIdQueryValidator()),
        Id<GetUserByIdQuery>(nameof(GetUserByIdQuery), nameof(GetUserByIdQuery.UserId), id => new GetUserByIdQuery { UserId = id }, new GetUserByIdQueryValidator())
    };

    [Fact]
    public async Task AllIdentifierValidators_PositiveIdentifier_ShouldSucceed()
    {
        foreach (var identifierCase in Cases)
        {
            var result = await identifierCase.Validate(1);

            Assert.True(result.IsValid, $"{identifierCase.Name}: id=1 gecerli olmaliydi.");
        }
    }

    [Fact]
    public async Task AllIdentifierValidators_ZeroOrNegativeIdentifier_ShouldFail()
    {
        foreach (var identifierCase in Cases)
        {
            var zero = await identifierCase.Validate(0);
            var negative = await identifierCase.Validate(-5);

            Assert.Contains(zero.Errors, error => error.PropertyName == identifierCase.PropertyName);
            Assert.Contains(negative.Errors, error => error.PropertyName == identifierCase.PropertyName);
        }
    }

    private static IdentifierCase Id<TRequest>(string name, string propertyName, Func<int, TRequest> factory, IValidator<TRequest> validator) =>
        new(name, propertyName, id => validator.ValidateAsync(factory(id)));
}
