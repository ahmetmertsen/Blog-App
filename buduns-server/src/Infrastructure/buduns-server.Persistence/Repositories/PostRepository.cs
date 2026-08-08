using buduns_server.Application.Dtos;
using buduns_server.Application.Repositories;
using buduns_server.Domain.Entities;
using buduns_server.Domain.Enums;
using buduns_server.Persistence.Context;
using buduns_server.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace buduns_server.Persistence.Repositories
{
    public class PostRepository : Repository<Post>, IPostRepository
    {
        private readonly BudunsDbContext _context;

        public PostRepository(BudunsDbContext context) : base(context)
        {
            _context = context;
        }

        public override Task<List<Post>> GetAllAsync() => VisiblePosts().Include(post => post.Tags).Include(post => post.Likes).ThenInclude(like => like.User).Include(post => post.Comments).Include(post => post.Bookmarks).OrderByDescending(post => post.CreatedAt).ThenByDescending(post => post.Id).AsNoTracking().ToListAsync();

        public override Task<Post?> GetByIdAsync(int id) => VisiblePosts().Include(post => post.User).Include(post => post.Tags).Include(post => post.Likes).ThenInclude(like => like.User).Include(post => post.Comments).Include(post => post.Bookmarks).AsNoTracking().FirstOrDefaultAsync(post => post.Id == id);

        public async Task<(List<PostDto> Items, int TotalCount)> GetPagedAsync(int page, int size, int? tagId, int? userId, string? search, string? sortBy, int? viewerUserId, CancellationToken cancellationToken = default)
        {
            var query = VisiblePosts();
            if (tagId.HasValue)
            {
                query = query.Where(post => post.Tags.Any(tag => tag.Id == tagId.Value && tag.isActive && !tag.isDeleted));
            }

            if (userId.HasValue)
            {
                query = query.Where(post => post.UserId == userId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                query = query.Where(post => EF.Functions.ILike(post.Content, $"%{keyword}%"));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await ProjectToPostDto(OrderPosts(query, sortBy).Skip((page - 1) * size).Take(size), viewerUserId).ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public async Task<(List<PostDto> Items, int TotalCount)> GetPagedByTagIdAsync(int tagId, int page, int size, int? viewerUserId, CancellationToken cancellationToken = default)
        {
            var query = VisiblePosts().Where(post => post.Tags.Any(tag => tag.Id == tagId && tag.isActive && !tag.isDeleted));
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await ProjectToPostDto(OrderPosts(query, "recent").Skip((page - 1) * size).Take(size), viewerUserId).ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public async Task<(List<PostDto> Items, int TotalCount)> GetPagedByUserIdAsync(int userId, int page, int size, int? viewerUserId, CancellationToken cancellationToken = default)
        {
            var query = VisiblePosts().Where(post => post.UserId == userId);
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await ProjectToPostDto(OrderPosts(query, "recent").Skip((page - 1) * size).Take(size), viewerUserId).ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public async Task<(List<PostDto> Items, int TotalCount)> GetPagedFollowingAsync(int userId, int page, int size, CancellationToken cancellationToken = default)
        {
            var query = VisiblePosts().Where(post => post.User.Followers.Any(follow => follow.FollowerId == userId && follow.isActive && !follow.isDeleted));
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await ProjectToPostDto(OrderPosts(query, "recent").Skip((page - 1) * size).Take(size), userId).ToListAsync(cancellationToken);
            return (items, totalCount);
        }

        public Task<PostDto?> GetDtoByIdAsync(int id, int? viewerUserId, CancellationToken cancellationToken = default) => ProjectToPostDto(VisiblePosts().Where(post => post.Id == id), viewerUserId).FirstOrDefaultAsync(cancellationToken);

        public Task<Post?> GetByIdWithTagsAsync(int id) => VisiblePosts().Include(post => post.Tags).FirstOrDefaultAsync(post => post.Id == id);

        public Task<bool> ExistsVisibleAsync(int id, CancellationToken cancellationToken = default) => VisiblePosts().AnyAsync(post => post.Id == id, cancellationToken);

        public Task<int?> GetVisibleOwnerIdAsync(int id, CancellationToken cancellationToken = default) => VisiblePosts().Where(post => post.Id == id).Select(post => (int?)post.UserId).FirstOrDefaultAsync(cancellationToken);

        private const double DailyLikeWeight = 0.4;
        private const double DailyCommentWeight = 0.6;

        /// <summary>
        /// Iki asamada calisir. Once gunun begeni/yorum olaylarindan yola cikip
        /// kazanan paylasimlar bulunur; ardindan yalnizca o paylasimlar icin
        /// toplam sayimlar cekilir.
        ///
        /// Paylasimdan baslayip her paylasim icin sayim yapmak, hicbir aktivite
        /// almamis paylasimlar da dahil tum tabloyu dolasmak demekti. Gunun
        /// olay sayisi paylasim sayisindan bagimsiz ve cok daha kucuk oldugu
        /// icin yon tersine cevrildi.
        /// </summary>
        public async Task<List<TopPostDto>> GetDailyTopPostsAsync(DateTime startDateUtc, DateTime endDateUtc, int limit, CancellationToken cancellationToken = default)
        {
            var safeLimit = Math.Clamp(limit, 1, 100);

            var ranked = await GetDailyRankedPostsAsync(startDateUtc, endDateUtc, safeLimit, cancellationToken);
            if (ranked.Count == 0)
            {
                return new List<TopPostDto>();
            }

            var postIds = ranked.Select(item => item.PostId).ToList();
            var totals = await VisiblePosts().AsNoTracking().Where(post => postIds.Contains(post.Id)).Select(post => new
            {
                post.Id,
                post.Content,
                post.UserId,
                UserName = post.User.UserName ?? string.Empty,
                UserFullName = post.User.FullName,
                UserImageUrl = post.User.ImageUrl,
                LikeCount = post.Likes.Count(like => like.User.Status != UserStatus.Banned && like.isActive && !like.isDeleted),
                CommentCount = post.Comments.Count(comment => comment.User.Status != UserStatus.Banned && comment.Status == CommentStatus.Published && comment.isActive && !comment.isDeleted),
                BookmarkCount = post.Bookmarks.Count(bookmark => bookmark.isActive && !bookmark.isDeleted)
            }).ToListAsync(cancellationToken);

            var totalsByPostId = totals.ToDictionary(item => item.Id);

            var result = new List<TopPostDto>(ranked.Count);
            foreach (var item in ranked)
            {
                // Siralama birinci asamadan gelir; sozluk yalnizca toplamlari tasir.
                if (!totalsByPostId.TryGetValue(item.PostId, out var total))
                {
                    continue;
                }

                result.Add(new TopPostDto
                {
                    PostId = total.Id,
                    Content = total.Content,
                    UserId = total.UserId,
                    UserName = total.UserName,
                    UserFullName = total.UserFullName,
                    UserImageUrl = total.UserImageUrl,
                    DailyLikeCount = item.DailyLikeCount,
                    DailyCommentCount = item.DailyCommentCount,
                    LikeCount = total.LikeCount,
                    CommentCount = total.CommentCount,
                    BookmarkCount = total.BookmarkCount,
                    Score = (item.DailyLikeCount * DailyLikeWeight) + (item.DailyCommentCount * DailyCommentWeight)
                });
            }

            return result;
        }

        private async Task<List<DailyRankedPost>> GetDailyRankedPostsAsync(DateTime startDateUtc, DateTime endDateUtc, int limit, CancellationToken cancellationToken)
        {
            // Kendi begenisi eskiden de gunluk sayima girmiyordu; yorumlarda
            // boyle bir istisna yok. Davranis birebir korunuyor.
            var likeEvents = _context.Likes.Where(like =>
                    like.CreatedAt >= startDateUtc && like.CreatedAt < endDateUtc &&
                    like.isActive && !like.isDeleted &&
                    like.UserId != like.Post.UserId &&
                    like.User.Status != UserStatus.Banned &&
                    // VisiblePosts() ile ayni kosullar; ifade agacinda metot
                    // cagrisi cevrilemedigi icin satir ici yazilmak zorunda.
                    like.Post.Status == PostStatus.Published && like.Post.isPublished && like.Post.isActive && !like.Post.isDeleted &&
                    like.Post.User.Status != UserStatus.Banned)
                .Select(like => new { like.PostId, PostCreatedAt = like.Post.CreatedAt, LikeCount = 1, CommentCount = 0 });

            var commentEvents = _context.Comments.Where(comment =>
                    comment.CreatedAt >= startDateUtc && comment.CreatedAt < endDateUtc &&
                    comment.Status == CommentStatus.Published &&
                    comment.isActive && !comment.isDeleted &&
                    comment.User.Status != UserStatus.Banned &&
                    comment.Post.Status == PostStatus.Published && comment.Post.isPublished && comment.Post.isActive && !comment.Post.isDeleted &&
                    comment.Post.User.Status != UserStatus.Banned)
                .Select(comment => new { comment.PostId, PostCreatedAt = comment.Post.CreatedAt, LikeCount = 0, CommentCount = 1 });

            return await likeEvents.Concat(commentEvents)
                // PostId benzersiz oldugu icin PostCreatedAt'i gruba tasimak
                // ek satir uretmez; esitlik bozmada kullanilabilmesini saglar.
                .GroupBy(activity => new { activity.PostId, activity.PostCreatedAt })
                .Select(group => new DailyRankedPost
                {
                    PostId = group.Key.PostId,
                    PostCreatedAt = group.Key.PostCreatedAt,
                    DailyLikeCount = group.Sum(activity => activity.LikeCount),
                    DailyCommentCount = group.Sum(activity => activity.CommentCount)
                })
                .OrderByDescending(item => (item.DailyLikeCount * DailyLikeWeight) + (item.DailyCommentCount * DailyCommentWeight))
                .ThenByDescending(item => item.DailyCommentCount)
                .ThenByDescending(item => item.DailyLikeCount)
                .ThenByDescending(item => item.PostCreatedAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }

        private sealed class DailyRankedPost
        {
            public int PostId { get; set; }
            public DateTime PostCreatedAt { get; set; }
            public int DailyLikeCount { get; set; }
            public int DailyCommentCount { get; set; }
        }

        private IQueryable<PostDto> ProjectToPostDto(IQueryable<Post> query, int? viewerUserId)
        {
            var hasViewer = viewerUserId.HasValue;
            var viewerId = viewerUserId ?? 0;

            return query.AsNoTracking().Select(post => new PostDto
            {
                Id = post.Id,
                Content = post.Content,
                UserId = post.UserId,
                UserName = post.User.UserName ?? string.Empty,
                UserFullName = post.User.FullName,
                UserImageUrl = post.User.ImageUrl,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdateAt,
                Tags = post.Tags.Where(tag => tag.isActive && !tag.isDeleted).OrderBy(tag => tag.Name).Select(tag => new TagDto { Id = tag.Id, Name = tag.Name }).ToList(),
                LikeCount = post.Likes.Count(like => like.isActive && !like.isDeleted && like.User.Status != UserStatus.Banned),
                CommentCount = post.Comments.Count(comment => comment.Status == CommentStatus.Published && comment.isActive && !comment.isDeleted && comment.User.Status != UserStatus.Banned),
                BookmarkCount = post.Bookmarks.Count(bookmark => bookmark.isActive && !bookmark.isDeleted),
                IsLiked = hasViewer && post.Likes.Any(like => like.UserId == viewerId && like.isActive && !like.isDeleted),
                IsBookmarked = hasViewer && post.Bookmarks.Any(bookmark => bookmark.UserId == viewerId && bookmark.isActive && !bookmark.isDeleted),
                IsOwner = hasViewer && post.UserId == viewerId,
                IsFollowingAuthor = hasViewer && post.User.Followers.Any(follow => follow.FollowerId == viewerId && follow.isActive && !follow.isDeleted)
            });
        }

        private IQueryable<Post> OrderPosts(IQueryable<Post> query, string? sortBy) => sortBy?.Trim().ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(post => post.CreatedAt).ThenBy(post => post.Id),
            "popular" => query.OrderByDescending(post => post.Likes.Count(like => like.isActive && !like.isDeleted && like.User.Status != UserStatus.Banned) + post.Comments.Count(comment => comment.Status == CommentStatus.Published && comment.isActive && !comment.isDeleted && comment.User.Status != UserStatus.Banned)).ThenByDescending(post => post.CreatedAt).ThenByDescending(post => post.Id),
            _ => query.OrderByDescending(post => post.CreatedAt).ThenByDescending(post => post.Id)
        };

        private IQueryable<Post> VisiblePosts() => _context.Posts.Where(post => post.Status == PostStatus.Published && post.isPublished && post.isActive && !post.isDeleted && post.User.Status != UserStatus.Banned);
    }
}
