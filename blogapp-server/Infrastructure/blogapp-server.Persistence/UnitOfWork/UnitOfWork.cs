using blogapp_server.Application.Repositories;
using blogapp_server.Application.UnitOfWork;
using blogapp_server.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BlogAppDbContext _context;

        public IBookmarkRepository BookmarkRepository { get; }
        public ICommentRepository CommentRepository { get; }
        public IFollowerRepository FollowerRepository { get; }
        public ILikeRepository LikeRepository { get; }
        public INotificationRepository NotificationRepository { get; }
        public IPostRepository PostRepository { get; }
        public ITagRepository TagRepository { get; }

        public UnitOfWork(BlogAppDbContext context, IBookmarkRepository bookmarkRepository, ICommentRepository commentRepository, IFollowerRepository followerRepository, ILikeRepository likeRepository, INotificationRepository notificationRepository, IPostRepository postRepository, ITagRepository tagRepository)
        {
            _context = context;
            BookmarkRepository = bookmarkRepository;
            CommentRepository = commentRepository;
            FollowerRepository = followerRepository;
            LikeRepository = likeRepository;
            NotificationRepository = notificationRepository;
            PostRepository = postRepository;
            TagRepository = tagRepository;
        }

        public void Dispose() => _context.Dispose();

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken) => await _context.SaveChangesAsync(cancellationToken);
    }
}
