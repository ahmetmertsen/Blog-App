using buduns_server.Domain.Entities;
using buduns_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace buduns_server.Persistence.Context
{
    public class BudunsDbContext : IdentityDbContext<User,Role, int>
    {
        public BudunsDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Follower> Followers { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Utility> Utilities { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ModerationAction> ModerationActions { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Endpoint> Endpoints { get; set; }
        public DbSet<AuthSession> AuthSessions { get; set; }
        public DbSet<VerificationChallenge> VerificationChallenges { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Post>(entity =>
            {
                entity.Property(post => post.Content)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(post => post.Status)
                    .HasDefaultValue(Domain.Enums.PostStatus.Published);

                entity.HasIndex(post => new { post.Status, post.isPublished, post.isActive, post.isDeleted, post.CreatedAt });
                entity.HasIndex(post => new { post.UserId, post.Status, post.CreatedAt });
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(user => user.Status)
                    .HasDefaultValue(Domain.Enums.UserStatus.Active);

                entity.Property(user => user.Status)
                    .HasConversion<string>();
            });

            modelBuilder.Entity<Tag>(entity =>
            {
                entity.Property(tag => tag.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(tag => tag.NormalizedName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(tag => tag.NormalizedName)
                    .IsUnique()
                    .HasDatabaseName("UX_Tags_NormalizedName_Active")
                    .HasFilter("\"isDeleted\" = false AND \"isActive\" = true");
            });

            modelBuilder.Entity<AuthSession>(entity =>
            {
                entity.HasKey(session => session.Id);

                entity.Property(session => session.RefreshTokenHash)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(session => session.DeviceName)
                    .HasMaxLength(100);

                entity.Property(session => session.UserAgent)
                    .HasMaxLength(500);

                entity.Property(session => session.IpAddress)
                    .HasMaxLength(64);

                entity.Property(session => session.RevokedReason)
                    .HasMaxLength(200);

                entity.HasOne(session => session.User)
                    .WithMany(user => user.AuthSessions)
                    .HasForeignKey(session => session.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(session => session.RefreshTokenHash)
                    .IsUnique();

                entity.HasIndex(session => new { session.UserId, session.RevokedAt });
                entity.HasIndex(session => session.TokenFamilyId);
                entity.HasIndex(session => session.ExpiresAt);
            });

            modelBuilder.Entity<VerificationChallenge>(entity =>
            {
                entity.Property(challenge => challenge.Purpose)
                    .HasConversion<string>();

                entity.Property(challenge => challenge.TargetEmail)
                    .HasMaxLength(256);

                entity.Property(challenge => challenge.CodeHash)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.HasOne(challenge => challenge.User)
                    .WithMany(user => user.VerificationChallenges)
                    .HasForeignKey(challenge => challenge.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(challenge => new { challenge.UserId, challenge.Purpose, challenge.TargetEmail, challenge.ConsumedAt });
                entity.HasIndex(challenge => challenge.ExpiresAt);
            });

            modelBuilder.Entity<Bookmark>(entity =>
            {
                entity.HasIndex(bookmark => new { bookmark.UserId, bookmark.PostId })
                    .IsUnique()
                    .HasDatabaseName("UX_Bookmarks_UserId_PostId");
            });

            modelBuilder.Entity<Like>(entity =>
            {
                entity.HasIndex(like => new { like.UserId, like.PostId })
                    .IsUnique()
                    .HasDatabaseName("UX_Likes_UserId_PostId");

                // Paylasim basina sayim yolu (daily-top50'nin ikinci asamasi ve
                // toplam begeni sayimlari) icin.
                entity.HasIndex(like => new { like.PostId, like.CreatedAt });

                // daily-top50'nin birinci asamasi gunun begenilerini tarih
                // araligiyla tarar; oradaki giris kolonu CreatedAt.
                entity.HasIndex(like => like.CreatedAt);
            });

            modelBuilder.Entity<Comment>(entity =>
            {
                entity.Property(comment => comment.Content)
                    .HasMaxLength(1000)
                    .IsRequired();

                entity.Property(comment => comment.Status)
                    .HasDefaultValue(Domain.Enums.CommentStatus.Published);

                entity.HasIndex(comment => new { comment.PostId, comment.Status, comment.CreatedAt });
                entity.HasIndex(comment => new { comment.UserId, comment.Status, comment.CreatedAt });

                // Likes.CreatedAt ile ayni gerekce: gunun yorumlarini tarih
                // araligiyla tarayan daily-top50 birinci asamasi icin.
                entity.HasIndex(comment => comment.CreatedAt);
            });

            modelBuilder.Entity<Follower>(entity =>
            {
                entity.ToTable(table => table.HasCheckConstraint("CK_Followers_DifferentUsers", "\"FollowerId\" <> \"FollowingId\""));

                entity.HasOne(f => f.FollowerUser)
                    .WithMany(u => u.Followings)
                    .HasForeignKey(f => f.FollowerId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(f => f.FollowingUser)
                    .WithMany(u => u.Followers)
                    .HasForeignKey(f => f.FollowingId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(x => new { x.FollowerId, x.FollowingId })
                    .IsUnique();
            });


            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.Type)
                    .HasConversion<string>();

                entity.HasOne(notification => notification.ActorUser)
                    .WithMany()
                    .HasForeignKey(notification => notification.ActorUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(notification => notification.Post)
                    .WithMany()
                    .HasForeignKey(notification => notification.PostId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(notification => notification.Comment)
                    .WithMany()
                    .HasForeignKey(notification => notification.CommentId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(notification => new { notification.UserId, notification.IsRead, notification.CreatedAt });
                entity.HasIndex(notification => new { notification.Type, notification.UserId, notification.ActorUserId, notification.PostId, notification.CreatedAt });
            });


            modelBuilder.Entity<Menu>(entity =>
            {
                entity.Property(menu => menu.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.HasIndex(menu => menu.Name)
                    .IsUnique()
                    .HasDatabaseName("UX_Menus_Name");
            });

            modelBuilder.Entity<Endpoint>(entity =>
            {
                entity.Property(endpoint => endpoint.Code)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(endpoint => endpoint.Definition)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(endpoint => endpoint.ActionType)
                    .HasMaxLength(20)
                    .IsRequired();

                entity.Property(endpoint => endpoint.HttpType)
                    .HasMaxLength(10)
                    .IsRequired();

                // Yetki kodu istek aninda menuden bagimsiz aranir; ayni kodun
                // iki kaydi olursa hangi rol kumesinin gecerli oldugu belirsizlesir.
                entity.HasIndex(endpoint => endpoint.Code)
                    .IsUnique()
                    .HasDatabaseName("UX_Endpoints_Code");
            });

            modelBuilder.Entity<Report>(entity =>
            {
                // PostgreSQL sistem sutunu xmin, iyimser eszamanlilik tokeni olarak
                // kullanilir. Npgsql saglayicisinda UseXminAsConcurrencyToken()
                // kaldirildigi icin golge ozellik olarak elle tanimlaniyor.
                entity.Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();

                entity.HasKey(r => r.Id);

                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_Reports_Target",
                    "(\"TargetType\" = 0 AND \"TargetPostId\" IS NOT NULL AND \"TargetUserId\" IS NULL AND \"TargetCommentId\" IS NULL) OR " +
                    "(\"TargetType\" = 1 AND \"TargetPostId\" IS NULL AND \"TargetUserId\" IS NOT NULL AND \"TargetCommentId\" IS NULL) OR " +
                    "(\"TargetType\" = 2 AND \"TargetPostId\" IS NULL AND \"TargetUserId\" IS NULL AND \"TargetCommentId\" IS NOT NULL)"));

                entity.Property(r => r.Description)
                    .HasMaxLength(1000);

                entity.Property(r => r.TargetOwnerUserNameSnapshot)
                    .HasMaxLength(256);

                entity.Property(r => r.TargetOwnerFullNameSnapshot)
                    .HasMaxLength(256);

                entity.Property(r => r.TargetContentSnapshot)
                    .HasMaxLength(1000);
                
                entity.Property(r => r.ReviewNote)
                    .HasMaxLength(1000);

                entity.HasOne(r => r.ReporterUser)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.TargetUser)
                    .WithMany()
                    .HasForeignKey(r => r.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ReviewedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.TargetPost)
                    .WithMany()
                    .HasForeignKey(r => r.TargetPostId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.TargetComment)
                    .WithMany(comment => comment.Reports)
                    .HasForeignKey(r => r.TargetCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new
                {
                    r.ReporterUserId,
                    r.TargetType,
                    r.TargetPostId,
                    r.TargetUserId,
                    r.TargetCommentId
                })
                .HasDatabaseName("IX_Reports_ReporterUserId_TargetType_TargetIds");

                entity.HasIndex(r => new { r.ReporterUserId, r.TargetPostId })
                    .IsUnique()
                    .HasFilter("\"TargetType\" = 0 AND \"Status\" IN (1, 2)");

                entity.HasIndex(r => new { r.ReporterUserId, r.TargetUserId })
                    .IsUnique()
                    .HasFilter("\"TargetType\" = 1 AND \"Status\" IN (1, 2)");

                entity.HasIndex(r => new { r.ReporterUserId, r.TargetCommentId })
                    .IsUnique()
                    .HasFilter("\"TargetType\" = 2 AND \"Status\" IN (1, 2)");

                entity.HasIndex(r => new
                {
                    r.TargetType,
                    r.TargetPostId,
                    r.TargetUserId,
                    r.Status
                });

                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.CreatedAt);
            });

            modelBuilder.Entity<ModerationAction>(entity =>
            {
                entity.HasKey(action => action.Id);

                entity.ToTable(table => table.HasCheckConstraint(
                    "CK_ModerationActions_Target",
                    "(\"TargetType\" = 0 AND \"TargetPostId\" IS NOT NULL AND \"TargetCommentId\" IS NULL) OR " +
                    "(\"TargetType\" = 1 AND \"TargetPostId\" IS NULL AND \"TargetUserId\" IS NOT NULL AND \"TargetCommentId\" IS NULL) OR " +
                    "(\"TargetType\" = 2 AND \"TargetPostId\" IS NULL AND \"TargetCommentId\" IS NOT NULL)"));

                entity.Property(action => action.Note)
                    .HasMaxLength(1000);

                entity.HasOne(action => action.Report)
                    .WithMany(report => report.ModerationActions)
                    .HasForeignKey(action => action.ReportId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(action => action.ModeratorUser)
                    .WithMany()
                    .HasForeignKey(action => action.ModeratorUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(action => action.TargetPost)
                    .WithMany()
                    .HasForeignKey(action => action.TargetPostId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(action => action.TargetUser)
                    .WithMany()
                    .HasForeignKey(action => action.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(action => action.TargetComment)
                    .WithMany(comment => comment.ModerationActions)
                    .HasForeignKey(action => action.TargetCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(action => action.ReportId);
                entity.HasIndex(action => action.ModeratorUserId);
                entity.HasIndex(action => new { action.TargetType, action.TargetPostId, action.TargetUserId, action.TargetCommentId });
                entity.HasIndex(action => action.CreatedAt);
            });
                


            base.OnModelCreating(modelBuilder);
        }

    }
}
