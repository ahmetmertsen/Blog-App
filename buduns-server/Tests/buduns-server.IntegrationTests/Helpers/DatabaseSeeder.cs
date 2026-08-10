using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using buduns_server.Domain.Enums;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace buduns_server.IntegrationTests.Helpers;

public static class DatabaseSeeder
{
    public const string DefaultPassword = "Integration123!";

    public static async Task<User> CreateUserAsync(IServiceProvider services, string userName, string fullName, params string[] roles)
    {
        var userManager = services.GetRequiredService<UserManager<User>>();
        var user = new User
        {
            UserName = userName,
            Email = $"{userName}@integration.test",
            FullName = fullName,
            EmailConfirmed = true,
            Status = UserStatus.Active
        };
        EnsureSucceeded(await userManager.CreateAsync(user, DefaultPassword), $"User could not be created: {userName}");
        if (roles.Length > 0)
        {
            EnsureSucceeded(await userManager.AddToRolesAsync(user, roles), $"Roles could not be assigned to user: {userName}");
        }

        return user;
    }

    public static async Task<Post> CreatePostAsync(IServiceProvider services, int userId, string content = "Integration test post")
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var post = new Post { UserId = userId, Content = content, isPublished = true, Status = PostStatus.Published, isActive = true, isDeleted = false, CreatedAt = DateTime.UtcNow };
        context.Posts.Add(post);
        await context.SaveChangesAsync();
        return post;
    }

    public static async Task<Comment> CreateCommentAsync(IServiceProvider services, int postId, int userId, string content = "Integration test comment")
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var comment = new Comment { PostId = postId, UserId = userId, Content = content, Status = CommentStatus.Published, isActive = true, isDeleted = false, CreatedAt = DateTime.UtcNow };
        context.Comments.Add(comment);
        await context.SaveChangesAsync();
        return comment;
    }

    public static async Task<Tag> CreateTagAsync(IServiceProvider services, string name)
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var tag = new Tag { Name = name, NormalizedName = name.Trim().ToUpperInvariant(), isActive = true, isDeleted = false, CreatedAt = DateTime.UtcNow, UpdateAt = DateTime.UtcNow };
        context.Tags.Add(tag);
        await context.SaveChangesAsync();
        return tag;
    }

    public static async Task<Report> CreateReportAsync(IServiceProvider services, Report report)
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        report.CreatedAt = report.CreatedAt == default ? DateTime.UtcNow : report.CreatedAt;
        report.isActive = true;
        report.isDeleted = false;
        context.Reports.Add(report);
        await context.SaveChangesAsync();
        return report;
    }

    public static async Task SetUserStatusAsync(IServiceProvider services, int userId, UserStatus status, DateTime? suspendedUntil = null)
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var user = await context.Users.SingleAsync(item => item.Id == userId);
        user.Status = status;
        user.SuspendedUntil = suspendedUntil;
        await context.SaveChangesAsync();
    }

    public static async Task SetEmailConfirmedAsync(IServiceProvider services, int userId, bool emailConfirmed)
    {
        var context = services.GetRequiredService<BudunsDbContext>();
        var user = await context.Users.SingleAsync(item => item.Id == userId);
        user.EmailConfirmed = emailConfirmed;
        await context.SaveChangesAsync();
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{message}. {string.Join(", ", result.Errors.Select(error => error.Description))}");
        }
    }
}
