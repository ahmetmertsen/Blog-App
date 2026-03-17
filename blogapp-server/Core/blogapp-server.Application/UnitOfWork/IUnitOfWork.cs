using blogapp_server.Application.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Application.UnitOfWork
{
    public interface IUnitOfWork
    {
        public IBookmarkRepository BookmarkRepository { get; }
        public ICommentRepository CommentRepository { get; }
        public IFollowerRepository FollowerRepository { get; }
        public ILikeRepository LikeRepository { get; }
        public INotificationRepository NotificationRepository { get; }
        public IPostRepository PostRepository { get; }
        public ITagRepository TagRepository { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
