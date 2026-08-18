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
    public class LikeRepository : Repository<Like>, ILikeRepository
    {
        private const string UserPostUniqueIndex = "UX_Likes_UserId_PostId";
        private readonly BudunsDbContext _context;

        public LikeRepository(BudunsDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(Like Like, bool Created)> CreateIfNotExistsAsync(Like like, CancellationToken cancellationToken)
        {
            var existingLike = await _context.Likes.FirstOrDefaultAsync(item => item.UserId == like.UserId && item.PostId == like.PostId, cancellationToken);
            if (existingLike != null)
            {
                if (existingLike.isActive && !existingLike.isDeleted)
                {
                    return (existingLike, false);
                }

                existingLike.isActive = true;
                existingLike.isDeleted = false;
                existingLike.CreatedAt = DateTime.UtcNow;
                await _context.SaveTranslatedAsync(cancellationToken);
                return (existingLike, true);
            }

            await _context.Likes.AddAsync(like, cancellationToken);

            try
            {
                // Bu flush bir commit degil: benzersizlik ihlalini
                // yakalayabilmek icin yazmanin veritabanina inmesi gerekiyor.
                // Commit sinirini TransactionBehavior yonetiyor.
                await _context.SaveTranslatedAsync(cancellationToken);
                return (like, true);
            }
            catch (DbUpdateException exception) when (IsDuplicateLike(exception))
            {
                _context.Entry(like).State = EntityState.Detached;

                existingLike = await _context.Likes.AsNoTracking().FirstOrDefaultAsync(item => item.UserId == like.UserId && item.PostId == like.PostId, cancellationToken);
                if (existingLike != null)
                {
                    return (existingLike, false);
                }

                throw;
            }
        }

        public async Task<bool> DeleteByUserAndPostAsync(int userId, int postId, CancellationToken cancellationToken)
        {
            var like = await _context.Likes.FirstOrDefaultAsync(item => item.UserId == userId && item.PostId == postId, cancellationToken);
            if (like == null)
            {
                return false;
            }

            // Kaydetme cagirana birakiliyor: silme, ayni istekteki diger
            // yazmalarla ayni sinirda kalici olmali.
            _context.Likes.Remove(like);
            return true;
        }

        public Task<Like?> GetByUserAndPostAsync(int userId, int postId, CancellationToken cancellationToken) => VisibleLikes().AsNoTracking().FirstOrDefaultAsync(like => like.UserId == userId && like.PostId == postId, cancellationToken);

        public async Task<(List<LikeDto> Items, int TotalCount)> GetPagedByPostIdAsync(int postId, int page, int size, CancellationToken cancellationToken)
        {
            var query = VisibleLikes().Where(like => like.PostId == postId && like.User.Status != UserStatus.Banned);
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(like => like.CreatedAt).ThenByDescending(like => like.Id).Skip((page - 1) * size).Take(size).AsNoTracking().Select(like => new LikeDto
            {
                Id = like.Id,
                UserId = like.UserId,
                UserName = like.User.UserName!,
                FullName = like.User.FullName,
                ImageUrl = like.User.ImageUrl,
                LikedAt = like.CreatedAt
            }).ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(List<LikedPostDto> Items, int TotalCount)> GetPagedByUserIdAsync(int userId, int page, int size, CancellationToken cancellationToken)
        {
            var query = VisibleLikes().Where(like => like.UserId == userId);
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(like => like.CreatedAt).ThenByDescending(like => like.Id).Skip((page - 1) * size).Take(size).AsNoTracking().Select(like => new LikedPostDto
            {
                LikeId = like.Id,
                LikedAt = like.CreatedAt,
                Post = new PostDto
                {
                    Id = like.Post.Id,
                    Content = like.Post.Content,
                    UserId = like.Post.UserId,
                    UserName = like.Post.User.UserName!,
                    UserFullName = like.Post.User.FullName,
                    UserImageUrl = like.Post.User.ImageUrl,
                    CreatedAt = like.Post.CreatedAt,
                    UpdatedAt = like.Post.UpdateAt,
                    Tags = like.Post.Tags.Where(tag => tag.isActive && !tag.isDeleted).Select(tag => new TagDto { Id = tag.Id, Name = tag.Name }).ToList(),
                    LikeCount = like.Post.Likes.Count(item => item.isActive && !item.isDeleted && item.User.Status != UserStatus.Banned),
                    CommentCount = like.Post.Comments.Count(comment => comment.Status == CommentStatus.Published && comment.isActive && !comment.isDeleted && comment.User.Status != UserStatus.Banned),
                    BookmarkCount = like.Post.Bookmarks.Count(bookmark => bookmark.isActive && !bookmark.isDeleted),
                    IsLiked = true,
                    IsBookmarked = like.Post.Bookmarks.Any(bookmark => bookmark.UserId == userId && bookmark.isActive && !bookmark.isDeleted),
                    IsOwner = like.Post.UserId == userId,
                    IsFollowingAuthor = like.Post.User.Followers.Any(follow => follow.FollowerId == userId && follow.isActive && !follow.isDeleted)
                }
            }).ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        private IQueryable<Like> VisibleLikes() => _context.Likes.Where(like => like.isActive && !like.isDeleted && like.Post.Status == PostStatus.Published && like.Post.isPublished && like.Post.isActive && !like.Post.isDeleted && like.Post.User.Status != UserStatus.Banned);

        private static bool IsDuplicateLike(DbUpdateException exception) => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: UserPostUniqueIndex };
    }
}
