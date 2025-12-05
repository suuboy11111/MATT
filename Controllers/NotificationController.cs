using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MaiAmTinhThuong.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationController(NotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications(int limit = 20)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { notifications = new List<Notification>(), unreadCount = 0 });

            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id, limit);
            var unreadCount = await _notificationService.GetUnreadCountAsync(user.Id);

            return Json(new { notifications, unreadCount });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _notificationService.MarkAllAsReadAsync(user.Id);
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _notificationService.DeleteAsync(id);
            return Json(new { success = result });
        }
    }
}

