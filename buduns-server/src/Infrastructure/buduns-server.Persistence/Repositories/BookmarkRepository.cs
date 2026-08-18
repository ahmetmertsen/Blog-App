using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using buduns_server.Persistence.Context;
using buduns_server.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace buduns_server.Persistence.Repositories
{
    public class BookmarkRepository : Repository<Bookmark>, IBookmarkRepository
    {
        private const string UserPostUniqueIndex = "UX_Bookmarks_UserId_PostId";
        private readonly BudunsDbContext _context;

        public BookmarkRepository(BudunsDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(Bookmark Bookmark, bool Created)> CreateIfNotExistsAsync(Bookmark bookmark, CancellationToken cancellationToken)
        {
            var existingBookmark = await _context.Bookmarks.FirstOrDefaultAsync(item => item.UserId == bookmark.UserId && item.PostId == bookmark.PostId, cancellationToken);
            if (existingBookmark != null)
            {
                if (existingBookmark.isActive && !existingBookmark.isDeleted)
                {
                    return (existingBookmark, false);
                }

                existingBookmark.isActive = true;
                existingBookmark.isDeleted = false;
                existingBookmark.CreatedAt = DateTime.UtcNow;
                await _context.SaveTranslatedAsync(cancellationToken);
                return (existingBookmark, true);
            }

            await _context.Bookmarks.AddAsync(bookmark, cancellationToken);

            try
            {
                // Bu flush bir commit degil: benzersizlik ihlalini
                // yakalayabilmek icin yazmanin veritabanina inmesi gerekiyor.
                // Commit sinirini TransactionBehavior yonetiyor.
                await _context.SaveTranslatedAsync(cancellationToken);
                return (bookmark, true);
            }
            catch (DbUpdateException exception) when (IsDuplicateBookmark(exception))
            {
                _context.Entry(bookmark).State = EntityState.Detached;

                existingBookmark = await _context.Bookmarks.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == bookmark.UserId && item.PostId == bookmark.PostId, cancellationToken);

                if (existingBookmark != null)
                {
                    return (existingBookmark, false);
                }

                throw;
            }
        }

        public async Task<bool> DeleteByUserAndPostAsync(int userId, int postId, CancellationToken cancellationToken)
        {
            var bookmark = await _context.Bookmarks.FirstOrDefaultAsync(item => item.UserId == userId && item.PostId == postId, cancellationToken);
            if (bookmark == null)
            {
                return false;
            }

            // Kaydetme cagirana birakiliyor: silme, ayni istekteki diger
            // yazmalarla ayni sinirda kalici olmali.
            _context.Bookmarks.Remove(bookmark);
            return true;
        }

        public async Task<(List<BookmarkDto> Items, int TotalCount)> GetPagedByUserIdAsync(int userId, int page, int size, CancellationToken cancellationToken)
        {
            var query = VisibleBookmarks(userId);
            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(bookmark => bookmark.CreatedAt)
                .ThenByDescending(bookmark => bookmark.Id)
                .Skip((page - 1) * size)
                .Take(size)
                .Select(bookmark => new BookmarkDto
                {
                    Id = bookmark.Id,
                    PostId = bookmark.PostId,
                    SavedAt = bookmark.CreatedAt,
                    Post = new PostDto
                    {
                        Id = bookmark.Post.Id,
                        Content = bookmark.Post.Content,
                        UserId = bookmark.Post.UserId,
                        UserName = bookmark.Post.User.UserName!,
                        UserFullName = bookmark.Post.User.FullName,
                        UserImageUrl = bookmark.Post.User.ImageUrl,
                        CreatedAt = bookmark.Post.CreatedAt,
                        UpdatedAt = bookmark.Post.UpdateAt,
                        Tags = bookmark.Post.Tags
                            .Where(tag => tag.isActive && !tag.isDeleted)
                            .Select(tag => new TagDto
                            {
                                Id = tag.Id,
                                Name = tag.Name
                            })
                            .ToList(),
                        LikeCount = bookmark.Post.Likes.Count(like => like.isActive && !like.isDeleted && like.User.Status != UserStatus.Banned),
                        CommentCount = bookmark.Post.Comments.Count(comment => comment.Status == CommentStatus.Published && comment.isActive && !comment.isDeleted && comment.User.Status != UserStatus.Banned),
                        BookmarkCount = bookmark.Post.Bookmarks.Count(item => item.isActive && !item.isDeleted),
                        IsLiked = bookmark.Post.Likes.Any(like => like.UserId == userId && like.isActive && !like.isDeleted),
                        IsBookmarked = true,
                        IsOwner = bookmark.Post.UserId == userId,
                        IsFollowingAuthor = bookmark.Post.User.Followers.Any(follow => follow.FollowerId == userId && follow.isActive && !follow.isDeleted)
                    }
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public Task<Bookmark?> GetByUserAndPostAsync(int userId, int postId, CancellationToken cancellationToken) => _context.Bookmarks.AsNoTracking().FirstOrDefaultAsync(bookmark => bookmark.UserId == userId && bookmark.PostId == postId && bookmark.isActive && !bookmark.isDeleted && bookmark.Post.Status == PostStatus.Published && bookmark.Post.isPublished && bookmark.Post.isActive && !bookmark.Post.isDeleted, cancellationToken);

        private IQueryable<Bookmark> VisibleBookmarks(int userId) =>
            _context.Bookmarks.Where(bookmark =>
                bookmark.UserId == userId &&
                bookmark.isActive &&
                !bookmark.isDeleted &&
                bookmark.Post.Status == PostStatus.Published &&
                bookmark.Post.isPublished &&
                bookmark.Post.isActive &&
                !bookmark.Post.isDeleted &&
                bookmark.Post.User.Status != UserStatus.Banned);

        private static bool IsDuplicateBookmark(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: UserPostUniqueIndex
            };
    }
}
