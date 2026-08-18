using buduns_server.Application.Exceptions;
using buduns_server.Application.Repositories;
using buduns_server.Application.UnitOfWork;
using buduns_server.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Persistence.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly BudunsDbContext _context;

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
        public IMenuRepository MenuRepository { get; }
        public IEndpointRepository EndpointRepository { get; }
        public IUserRepository UserRepository { get; }


        public UnitOfWork(BudunsDbContext context, IBookmarkRepository bookmarkRepository, ICommentRepository commentRepository, IFollowerRepository followerRepository, ILikeRepository likeRepository, INotificationRepository notificationRepository, IPostRepository postRepository, ITagRepository tagRepository, IUtilityRepository utilityRepository, IReportRepository reportRepository, IModerationActionRepository moderationActionRepository, IEndpointRepository endpointRepository, IMenuRepository menuRepository, IUserRepository userRepository)
        {
            _context = context;
            BookmarkRepository = bookmarkRepository;
            CommentRepository = commentRepository;
            FollowerRepository = followerRepository;
            LikeRepository = likeRepository;
            NotificationRepository = notificationRepository;
            PostRepository = postRepository;
            TagRepository = tagRepository;
            UtilityRepository = utilityRepository;
            ReportRepository = reportRepository;
            ModerationActionRepository = moderationActionRepository;
            EndpointRepository = endpointRepository;
            MenuRepository = menuRepository;
            UserRepository = userRepository;
        }

        // EF Core'a ozgu eszamanlilik istisnasi burada uygulama seviyesine
        // cevriliyor; boylece Application katmani EF Core'a bagli kalmiyor.
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyConflictException();
            }
        }
    }
}
