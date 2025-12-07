using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MaiAmTinhThuong.Models;

namespace MaiAmTinhThuong.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Các DbSet hiện tại
        public DbSet<MaiAm> MaiAms { get; set; }
        public DbSet<SupportRequest> SupportRequests { get; set; }
        public DbSet<Supporter> Supporters { get; set; }
        public DbSet<SupportType> SupportTypes { get; set; }
        public DbSet<SupporterSupportType> SupporterSupportTypes { get; set; }
        public DbSet<SupporterMaiAm> SupporterMaiAms { get; set; }

        public DbSet<TransactionHistory> TransactionHistories { get; set; }
        public DbSet<ContactResponse> ContactResponses { get; set; }
        public DbSet<VinhDanh> VinhDanhs { get; set; }
        public DbSet<ChungNhanQuyenGop> ChungNhanQuyenGops { get; set; }
        public DbSet<BotRule> BotRules { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        // Thêm DbSet cho blog
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Like> Likes { get; set; }
        
        // Data Protection Keys (cho OAuth state encryption)
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        // Cấu hình mối quan hệ giữa BlogPost và ApplicationUser
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cấu hình mối quan hệ giữa BlogPost và ApplicationUser
            builder.Entity<BlogPost>()
                .HasOne(b => b.Author)
                .WithMany()  // Một người dùng có thể viết nhiều bài viết
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);  // Khi người dùng bị xóa, chỉ set AuthorId = NULL

            // Cấu hình quan hệ giữa Comment và ApplicationUser
            builder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany()  // Một người dùng có thể bình luận nhiều lần
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Cấu hình quan hệ giữa Like và ApplicationUser
            builder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()  // Một người dùng có thể thích nhiều bài viết
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);  // Khi người dùng bị xóa, các lượt thích của họ cũng bị xóa

            // Quan hệ SupporterSupportType nhiều-nhiều
            builder.Entity<SupporterSupportType>()
                .HasKey(sst => new { sst.SupporterId, sst.SupportTypeId });

            builder.Entity<SupporterSupportType>()
                .HasOne(sst => sst.Supporter)
                .WithMany(s => s.SupporterSupportTypes)
                .HasForeignKey(sst => sst.SupporterId);

            builder.Entity<SupporterSupportType>()
                .HasOne(sst => sst.SupportType)
                .WithMany(st => st.SupporterSupportTypes)
                .HasForeignKey(sst => sst.SupportTypeId);

            // Quan hệ SupporterMaiAm nhiều-nhiều
            builder.Entity<SupporterMaiAm>()
                .HasKey(sma => new { sma.SupporterId, sma.MaiAmId });

            builder.Entity<SupporterMaiAm>()
                .HasOne(sma => sma.Supporter)
                .WithMany(s => s.SupporterMaiAms)
                .HasForeignKey(sma => sma.SupporterId);

            builder.Entity<SupporterMaiAm>()
                .HasOne(sma => sma.MaiAm)
                .WithMany(m => m.SupporterMaiAms)
                .HasForeignKey(sma => sma.MaiAmId);

            // Quan hệ một-nhiều giữa SupportRequest và MaiAm
            builder.Entity<SupportRequest>()
                .HasOne(sr => sr.MaiAm)
                .WithMany(m => m.SupportRequests)
                .HasForeignKey(sr => sr.MaiAmId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        // Override SaveChangesAsync để đảm bảo tất cả DateTime đều là UTC (PostgreSQL yêu cầu)
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added || 
                           e.State == Microsoft.EntityFrameworkCore.EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                // Xử lý ApplicationUser
                if (entityEntry.Entity is ApplicationUser user)
                {
                    // Đảm bảo CreatedAt là UTC
                    if (user.CreatedAt.HasValue && user.CreatedAt.Value.Kind != DateTimeKind.Utc)
                    {
                        user.CreatedAt = user.CreatedAt.Value.ToUniversalTime();
                    }

                    // Đảm bảo UpdatedAt là UTC
                    if (user.UpdatedAt.HasValue && user.UpdatedAt.Value.Kind != DateTimeKind.Utc)
                    {
                        user.UpdatedAt = user.UpdatedAt.Value.ToUniversalTime();
                    }

                    // Đảm bảo DateOfBirth là UTC (chỉ lấy date part)
                    if (user.DateOfBirth.HasValue)
                    {
                        var dateOnly = user.DateOfBirth.Value.Date;
                        user.DateOfBirth = new DateTime(dateOnly.Ticks, DateTimeKind.Utc);
                    }
                }
                
                // Xử lý BlogPost
                if (entityEntry.Entity is BlogPost blogPost)
                {
                    // Đảm bảo CreatedAt là UTC
                    if (blogPost.CreatedAt.Kind != DateTimeKind.Utc)
                    {
                        blogPost.CreatedAt = blogPost.CreatedAt.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(blogPost.CreatedAt, DateTimeKind.Utc)
                            : blogPost.CreatedAt.ToUniversalTime();
                    }
                }
                
                // Xử lý Notification
                if (entityEntry.Entity is Notification notification)
                {
                    // Đảm bảo CreatedAt là UTC
                    if (notification.CreatedAt.Kind != DateTimeKind.Utc)
                    {
                        notification.CreatedAt = notification.CreatedAt.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(notification.CreatedAt, DateTimeKind.Utc)
                            : notification.CreatedAt.ToUniversalTime();
                    }
                }
                
                // Xử lý Comment
                if (entityEntry.Entity is Comment comment)
                {
                    // Đảm bảo CreatedAt là UTC
                    if (comment.CreatedAt.Kind != DateTimeKind.Utc)
                    {
                        comment.CreatedAt = comment.CreatedAt.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(comment.CreatedAt, DateTimeKind.Utc)
                            : comment.CreatedAt.ToUniversalTime();
                    }
                }
                
                // Xử lý SupportRequest
                if (entityEntry.Entity is SupportRequest supportRequest)
                {
                    // Đảm bảo CreatedDate là UTC
                    if (supportRequest.CreatedDate.Kind != DateTimeKind.Utc)
                    {
                        supportRequest.CreatedDate = supportRequest.CreatedDate.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(supportRequest.CreatedDate, DateTimeKind.Utc)
                            : supportRequest.CreatedDate.ToUniversalTime();
                    }
                    
                    // Đảm bảo UpdatedDate là UTC
                    if (supportRequest.UpdatedDate.Kind != DateTimeKind.Utc)
                    {
                        supportRequest.UpdatedDate = supportRequest.UpdatedDate.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(supportRequest.UpdatedDate, DateTimeKind.Utc)
                            : supportRequest.UpdatedDate.ToUniversalTime();
                    }
                }
                
                // Xử lý Supporter
                if (entityEntry.Entity is Supporter supporter)
                {
                    // Đảm bảo CreatedDate là UTC
                    if (supporter.CreatedDate.Kind != DateTimeKind.Utc)
                    {
                        supporter.CreatedDate = supporter.CreatedDate.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(supporter.CreatedDate, DateTimeKind.Utc)
                            : supporter.CreatedDate.ToUniversalTime();
                    }
                    
                    // Đảm bảo UpdatedDate là UTC
                    if (supporter.UpdatedDate.Kind != DateTimeKind.Utc)
                    {
                        supporter.UpdatedDate = supporter.UpdatedDate.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(supporter.UpdatedDate, DateTimeKind.Utc)
                            : supporter.UpdatedDate.ToUniversalTime();
                    }
                }
                
                // Xử lý MaiAm
                if (entityEntry.Entity is MaiAm maiAm)
                {
                    // Đảm bảo CreatedDate là UTC
                    if (maiAm.CreatedDate.Kind != DateTimeKind.Utc)
                    {
                        maiAm.CreatedDate = maiAm.CreatedDate.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(maiAm.CreatedDate, DateTimeKind.Utc)
                            : maiAm.CreatedDate.ToUniversalTime();
                    }
                    
                    // Đảm bảo UpdatedDate là UTC
                    if (maiAm.UpdatedDate.Kind != DateTimeKind.Utc)
                    {
                        maiAm.UpdatedDate = maiAm.UpdatedDate.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(maiAm.UpdatedDate, DateTimeKind.Utc)
                            : maiAm.UpdatedDate.ToUniversalTime();
                    }
                }
                
                // Xử lý TransactionHistory
                if (entityEntry.Entity is TransactionHistory transaction)
                {
                    // Đảm bảo TransactionDate là UTC
                    if (transaction.TransactionDate.Kind != DateTimeKind.Utc)
                    {
                        transaction.TransactionDate = transaction.TransactionDate.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(transaction.TransactionDate, DateTimeKind.Utc)
                            : transaction.TransactionDate.ToUniversalTime();
                    }
                }
                
                // Xử lý VinhDanh
                if (entityEntry.Entity is VinhDanh vinhDanh)
                {
                    // Đảm bảo NgayVinhDanh là UTC
                    if (vinhDanh.NgayVinhDanh.Kind != DateTimeKind.Utc)
                    {
                        vinhDanh.NgayVinhDanh = vinhDanh.NgayVinhDanh.Kind == DateTimeKind.Unspecified 
                            ? DateTime.SpecifyKind(vinhDanh.NgayVinhDanh, DateTimeKind.Utc)
                            : vinhDanh.NgayVinhDanh.ToUniversalTime();
                    }
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
