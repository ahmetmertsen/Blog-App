using blogapp_server.Domain.Entities;
using blogapp_server.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blogapp_server.Persistence.Context
{
    public class BlogAppDbContext : IdentityDbContext<User,Role, int>
    {
        public BlogAppDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Post> Posts { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Like> Likes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Follower> Followers { get; set; }
        public DbSet<Bookmark> Bookmarks { get; set; }
        public DbSet<Utility> Utilities { get; set; }
        public DbSet<Report> Reports { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Follower>(entity =>
            {
                entity.HasOne(f => f.FollowerUser)
                    .WithMany(u => u.Followings)
                    .HasForeignKey(f => f.FollowerId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(f => f.FollowingUser)
                    .WithMany(u => u.Followers)
                    .HasForeignKey(f => f.FollowingId)
                    .OnDelete(DeleteBehavior.NoAction);
            });


            modelBuilder.Entity<Follower>(entity =>
            {
                entity.HasIndex(x => new { x.FollowerId, x.FollowingId })
                    .IsUnique();
            });


            modelBuilder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.Type)
                    .HasConversion<string>();
            });


            modelBuilder.Entity<Report>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Description)
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

                entity.HasIndex(r => new
                {
                    r.ReporterUserId,
                    r.TargetType,
                    r.TargetPostId,
                    r.TargetUserId
                });

                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.CreatedAt);
            });
                


            base.OnModelCreating(modelBuilder);
        }

    }
}
