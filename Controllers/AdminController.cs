using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaiAmTinhThuong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public IActionResult AdminDashboard()
        {
            return View();
        }

        // Hiển thị các phản hồi từ form liên hệ
        public IActionResult ContactResponses()
        {
            var responses = _context.ContactResponses.OrderByDescending(c => c.SubmittedAt).ToList();
            return View(responses);
        }

       
        [HttpPost]
        public async Task<IActionResult> DeleteContactResponse(int id)
        {
            var contactResponse = await _context.ContactResponses.FindAsync(id);
            if (contactResponse != null)
            {
                _context.ContactResponses.Remove(contactResponse);
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Đã xóa phản hồi liên hệ thành công!";
            return RedirectToAction("ContactResponses");
        }

        // Quản lý tài khoản người dùng
        public async Task<IActionResult> ManageAccounts()
        {
            var users = await _context.Users
                                       .Where(u => u.Role != "Admin")
                                       .ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                    TempData["Message"] = "Tài khoản đã được xóa!";
            }
            return RedirectToAction("ManageAccounts");
        }

        [HttpPost]
        public async Task<IActionResult> Block(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                    TempData["Message"] = "Tài khoản đã bị chặn!";
            }
            return RedirectToAction("ManageAccounts");
        }

        [HttpPost]
        public async Task<IActionResult> Unblock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.LockoutEnabled = false;
                user.LockoutEnd = null;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                    TempData["Message"] = "Tài khoản đã được mở khóa!";
            }
            return RedirectToAction("ManageAccounts");
        }

        // ========== HỖ TRỢ ADMIN: SUPPORTER ==========
        public async Task<IActionResult> ManageSupporters()
        {
            var supporters = await _context.Supporters.ToListAsync();
            return View(supporters);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSupporter(int id)
        {
            var supporter = await _context.Supporters.FindAsync(id);
            if (supporter != null)
            {
                _context.Supporters.Remove(supporter);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageSupporters");
        }

      

        [HttpPost]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            var request = await _context.SupportRequests.FindAsync(id);
            if (request != null)
            {
                _context.SupportRequests.Remove(request);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageSupportRequests");
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.SupportRequests.FindAsync(id);
            if (request != null && !request.IsApproved)
            {
                request.IsApproved = true;
                _context.Update(request);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Hồ sơ đã được duyệt!";
            }
            return RedirectToAction("ManageSupportRequests");
        }

        // Hiển thị trang quản lý bài viết
        public async Task<IActionResult> ManageBlogPosts()
        {
            var blogPosts = await _context.BlogPosts
                .Include(b => b.Author)  // Bao gồm thông tin tác giả
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            // Phân loại bài viết theo trạng thái duyệt
            var pendingPosts = blogPosts.Where(b => !b.IsApproved).ToList();  // Bài viết chưa duyệt
            var approvedPosts = blogPosts.Where(b => b.IsApproved).ToList();  // Bài viết đã duyệt

            ViewBag.PendingPosts = pendingPosts;
            ViewBag.ApprovedPosts = approvedPosts;

            return View();  // Trả về view quản lý bài viết
        }

        // Duyệt bài viết
        [HttpPost]
        public async Task<IActionResult> ApproveBlogPost(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost != null)
            {
                blogPost.IsApproved = true;  // Cập nhật trạng thái duyệt
                _context.Update(blogPost);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Bài viết đã được duyệt!";
            }
            return RedirectToAction("ManageBlogPosts");
        }

        // Xóa bài viết
        [HttpPost]
        public async Task<IActionResult> DeleteBlogPost(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost != null)
            {
                _context.BlogPosts.Remove(blogPost);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageBlogPosts");
        }
    }
}
