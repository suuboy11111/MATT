using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace MaiAmTinhThuong.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<Notification> CreateAsync(Notification notification)
        {
            notification.CreatedAt = DateTime.Now;
            notification.IsRead = false;
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int limit = 20)
        {
            try
            {
                return await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(limit)
                    .ToListAsync();
            }
            catch
            {
                // Nếu bảng chưa tồn tại, trả về danh sách rỗng
                return new List<Notification>();
            }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            try
            {
                return await _context.Notifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            catch
            {
                // Nếu bảng chưa tồn tại, trả về 0
                return 0;
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification == null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        // Helper methods để tạo thông báo tự động
        public async Task NotifyProfileApprovedAsync(string userId, string profileName)
        {
            await CreateAsync(new Notification
            {
                UserId = userId,
                Title = "Hồ sơ đã được duyệt",
                Message = $"Hồ sơ '{profileName}' của bạn đã được duyệt thành công.",
                Type = "Success",
                Link = "/Account/Profile"
            });
        }

        public async Task NotifySupportRequestApprovedAsync(string userId, string requestName)
        {
            await CreateAsync(new Notification
            {
                UserId = userId,
                Title = "Hồ sơ cần hỗ trợ đã được duyệt",
                Message = $"Hồ sơ '{requestName}' đã được duyệt và sẽ được xem xét hỗ trợ.",
                Type = "Success"
            });
        }

        public async Task NotifySupporterApprovedAsync(string userId, string supporterName)
        {
            await CreateAsync(new Notification
            {
                UserId = userId,
                Title = "Đăng ký người hỗ trợ đã được duyệt",
                Message = $"Đăng ký người hỗ trợ '{supporterName}' đã được duyệt thành công.",
                Type = "Success"
            });
        }

        public async Task NotifySupportRequestCreatedAsync(string userId, string requestName)
        {
            await CreateAsync(new Notification
            {
                UserId = userId,
                Title = "Đăng ký hồ sơ thành công",
                Message = $"Hồ sơ '{requestName}' của bạn đã được gửi thành công. Vui lòng chờ admin duyệt.",
                Type = "Info",
                Link = "/SupportRequest/CreateRequest"
            });
        }

        public async Task NotifySupporterCreatedAsync(string userId, string supporterName)
        {
            await CreateAsync(new Notification
            {
                UserId = userId,
                Title = "Đăng ký người hỗ trợ thành công",
                Message = $"Đăng ký người hỗ trợ '{supporterName}' đã được gửi thành công. Vui lòng chờ admin duyệt.",
                Type = "Info",
                Link = "/Supporter/CreateSupporter"
            });
        }

        // Thông báo cho tất cả admin
        public async Task NotifyAllAdminsAsync(string title, string message, string type = "Info", string link = null)
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in admins)
            {
                await CreateAsync(new Notification
                {
                    UserId = admin.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    Link = link
                });
            }
        }

        // Thông báo cho admin khi có hồ sơ mới
        public async Task NotifyAdminNewSupportRequestAsync(string requestName, int requestId)
        {
            await NotifyAllAdminsAsync(
                "Hồ sơ cần hỗ trợ mới",
                $"Có hồ sơ mới '{requestName}' cần được duyệt.",
                "Warning",
                $"/Admin/ManageSupportRequests"
            );
        }

        // Thông báo cho admin khi có người hỗ trợ mới
        public async Task NotifyAdminNewSupporterAsync(string supporterName, int supporterId)
        {
            await NotifyAllAdminsAsync(
                "Đăng ký người hỗ trợ mới",
                $"Có đăng ký người hỗ trợ mới '{supporterName}' cần được duyệt.",
                "Warning",
                $"/Admin/ManageSupporters"
            );
        }

        // Thông báo cho admin khi có bài viết mới
        public async Task NotifyAdminNewBlogPostAsync(string postTitle, int postId, string authorName)
        {
            await NotifyAllAdminsAsync(
                "Bài viết mới cần duyệt",
                $"Bài viết '{postTitle}' của {authorName} cần được duyệt.",
                "Warning",
                $"/Admin/ManageBlogPosts"
            );
        }

        // Thông báo cho user khi tạo bài viết
        public async Task NotifyBlogPostCreatedAsync(string userId, string postTitle)
        {
            await CreateAsync(new Notification
            {
                UserId = userId,
                Title = "Bài viết đã được gửi",
                Message = $"Bài viết '{postTitle}' của bạn đã được gửi thành công. Vui lòng chờ admin duyệt.",
                Type = "Info",
                Link = "/Blog"
            });
        }

        // Thông báo cho user khi bài viết được duyệt
        public async Task NotifyBlogPostApprovedAsync(string userId, string postTitle)
        {
            await CreateAsync(new Notification
            {
                UserId = userId,
                Title = "Bài viết đã được duyệt",
                Message = $"Bài viết '{postTitle}' của bạn đã được duyệt và đã được đăng.",
                Type = "Success",
                Link = "/Blog"
            });
        }

        // Thông báo cho admin khi có liên hệ mới
        public async Task NotifyAdminNewContactAsync(string contactName, string contactEmail, int contactId)
        {
            await NotifyAllAdminsAsync(
                "Liên hệ mới",
                $"Có liên hệ mới từ {contactName} ({contactEmail}).",
                "Info",
                $"/Admin/ContactResponses"
            );
        }
    }
}

