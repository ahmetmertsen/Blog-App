using buduns_server.Application.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Application.UnitOfWork
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
        public IUtilityRepository UtilityRepository { get; }
        public IReportRepository ReportRepository { get; }
        public IModerationActionRepository ModerationActionRepository { get; }
        public IEndpointRepository EndpointRepository { get; }
        public IMenuRepository MenuRepository { get; }
        public IUserRepository UserRepository { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
