using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MaiAmTinhThuong.Models;

namespace MaiAmTinhThuong.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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
    }
}
