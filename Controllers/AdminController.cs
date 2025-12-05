using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using MaiAmTinhThuong.Helpers;
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
        private readonly SupportRequestService _supportRequestService;
        private readonly SupporterService _supporterService;

        public AdminController(ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            SupportRequestService supportRequestService,
            SupporterService supporterService)
        {
            _context = context;
            _userManager = userManager;
            _supportRequestService = supportRequestService;
            _supporterService = supporterService;
        }

        public async Task<IActionResult> AdminDashboard()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalSupportRequests = await _supportRequestService.GetCountAsync(),
                PendingSupportRequests = await _supportRequestService.GetPendingCountAsync(),
                TotalSupporters = await _supporterService.GetCountAsync(),
                ApprovedSupporters = await _supporterService.GetApprovedCountAsync(),
                TotalMaiAms = await _context.MaiAms.CountAsync(),
                TotalBlogPosts = await _context.BlogPosts.CountAsync(),
                PendingBlogPosts = await _context.BlogPosts.CountAsync(b => !b.IsApproved),
                TotalTransactions = await _context.TransactionHistories.CountAsync(),
                TotalAmount = await _context.TransactionHistories.SumAsync(t => t.Amount)
            };

            ViewBag.Stats = stats;
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
        public async Task<IActionResult> ManageAccounts(string searchString, string filterRole, int? page, int? pageSize)
        {
            var query = _context.Users
                .Where(u => u.Role != "Admin")
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.FullName.Contains(searchString) || 
                                       u.Email.Contains(searchString) ||
                                       (u.PhoneNumber != null && u.PhoneNumber.Contains(searchString)));
            }

            // Filter by role
            if (!string.IsNullOrEmpty(filterRole))
            {
                query = query.Where(u => u.Role == filterRole);
            }

            // Get pagination parameters
            var currentPage = PaginationHelper.GetPageNumber(page);
            var currentPageSize = PaginationHelper.GetPageSize(pageSize, 10);
            var totalItems = await query.CountAsync();

            // Pagination
            var users = await query
                .OrderByDescending(u => u.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(u => u.Email)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToListAsync();

            var pagedResult = new PagedResult<ApplicationUser>(users, totalItems, currentPage, currentPageSize);

            ViewBag.SearchString = searchString;
            ViewBag.FilterRole = filterRole;
            ViewBag.PageSizeOptions = PaginationHelper.GetPageSizeOptions();
            ViewBag.Roles = new List<string> { "NguoiHoTro", "NguoiCanHoTro" };

            return View(pagedResult);
        }

        // Quản lý hồ sơ cần hỗ trợ
        public async Task<IActionResult> ManageSupportRequests(string searchString, string filterApproved, string filterMaiAm, int? page, int? pageSize)
        {
            var query = _context.SupportRequests
                .Include(sr => sr.MaiAm)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(sr => sr.Name.Contains(searchString) || 
                                         sr.PhoneNumber.Contains(searchString) ||
                                         (sr.Address != null && sr.Address.Contains(searchString)));
            }

            // Filter by approval status
            if (!string.IsNullOrEmpty(filterApproved))
            {
                if (filterApproved == "approved")
                    query = query.Where(sr => sr.IsApproved);
                else if (filterApproved == "pending")
                    query = query.Where(sr => !sr.IsApproved);
            }

            // Filter by MaiAm
            if (!string.IsNullOrEmpty(filterMaiAm) && int.TryParse(filterMaiAm, out int maiAmId))
            {
                query = query.Where(sr => sr.MaiAmId == maiAmId);
            }

            // Get pagination parameters
            var currentPage = PaginationHelper.GetPageNumber(page);
            var currentPageSize = PaginationHelper.GetPageSize(pageSize, 10);
            var totalItems = await query.CountAsync();

            // Pagination
            var requests = await query
                .OrderByDescending(sr => sr.CreatedDate)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToListAsync();

            var pagedResult = new PagedResult<SupportRequest>(requests, totalItems, currentPage, currentPageSize);

            ViewBag.SearchString = searchString;
            ViewBag.FilterApproved = filterApproved;
            ViewBag.FilterMaiAm = filterMaiAm;
            ViewBag.PageSizeOptions = PaginationHelper.GetPageSizeOptions();
            ViewBag.MaiAms = await _context.MaiAms.ToListAsync();

            return View(pagedResult);
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
        public async Task<IActionResult> ManageSupporters(string searchString, string filterApproved, int? page, int? pageSize)
        {
            var query = _context.Supporters
                .Include(s => s.MaiAm)
                .Include(s => s.SupporterSupportTypes)
                    .ThenInclude(sst => sst.SupportType)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(s => s.Name.Contains(searchString) || 
                                        s.PhoneNumber.Contains(searchString) ||
                                        (s.Address != null && s.Address.Contains(searchString)));
            }

            // Filter by approval status
            if (!string.IsNullOrEmpty(filterApproved))
            {
                if (filterApproved == "approved")
                    query = query.Where(s => s.IsApproved);
                else if (filterApproved == "pending")
                    query = query.Where(s => !s.IsApproved);
            }

            // Get pagination parameters
            var currentPage = Helpers.PaginationHelper.GetPageNumber(page);
            var currentPageSize = Helpers.PaginationHelper.GetPageSize(pageSize, 10);
            var totalItems = await query.CountAsync();

            // Pagination
            var supporters = await query
                .OrderByDescending(s => s.CreatedDate)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToListAsync();

            var pagedResult = new Helpers.PagedResult<Supporter>(supporters, totalItems, currentPage, currentPageSize);

            ViewBag.SearchString = searchString;
            ViewBag.FilterApproved = filterApproved;
            ViewBag.PageSizeOptions = PaginationHelper.GetPageSizeOptions();

            return View("ManageSupportersList", pagedResult);
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
            var request = await _context.SupportRequests
                .Include(r => r.MaiAm)
                .FirstOrDefaultAsync(r => r.Id == id);
            
            if (request != null && !request.IsApproved)
            {
                request.IsApproved = true;
                _context.Update(request);
                await _context.SaveChangesAsync();
                
                // Gửi thông báo cho user nếu có user liên kết
                var notificationService = HttpContext.RequestServices.GetRequiredService<Services.NotificationService>();
                
                // Tìm user theo phone number (chính xác hoặc chứa)
                var user = await _userManager.Users.FirstOrDefaultAsync(u => 
                    (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber == request.PhoneNumber) ||
                    (!string.IsNullOrEmpty(u.PhoneNumber2) && u.PhoneNumber2 == request.PhoneNumber) ||
                    (!string.IsNullOrEmpty(request.PhoneNumber) && !string.IsNullOrEmpty(u.Email) && u.Email.Contains(request.PhoneNumber)));
                
                // Nếu không tìm thấy, thử tìm theo tên (nếu trùng tên và có phone number tương tự)
                if (user == null && !string.IsNullOrEmpty(request.PhoneNumber))
                {
                    user = await _userManager.Users.FirstOrDefaultAsync(u => 
                        u.FullName == request.Name && 
                        (!string.IsNullOrEmpty(u.PhoneNumber) && (u.PhoneNumber.Contains(request.PhoneNumber) || request.PhoneNumber.Contains(u.PhoneNumber))));
                }
                
                if (user != null)
                {
                    await notificationService.NotifySupportRequestApprovedAsync(user.Id, request.Name);
                }
                else
                {
                    // Nếu không tìm thấy user, vẫn gửi thông báo cho tất cả user có phone number tương tự
                    var similarUsers = await _userManager.Users
                        .Where(u => !string.IsNullOrEmpty(request.PhoneNumber) && 
                                   ((!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Contains(request.PhoneNumber.Substring(request.PhoneNumber.Length - 4))) ||
                                    (!string.IsNullOrEmpty(u.PhoneNumber2) && u.PhoneNumber2.Contains(request.PhoneNumber.Substring(request.PhoneNumber.Length - 4)))))
                        .ToListAsync();
                    
                    foreach (var similarUser in similarUsers)
                    {
                        await notificationService.NotifySupportRequestApprovedAsync(similarUser.Id, request.Name);
                    }
                }
                
                TempData["Message"] = "Hồ sơ đã được duyệt!";
            }
            return RedirectToAction("ManageSupportRequests");
        }

        // Hiển thị trang quản lý bài viết
        public async Task<IActionResult> ManageBlogPosts(string searchString, string filterApproved, int? page, int? pageSize)
        {
            var query = _context.BlogPosts
                .Include(b => b.Author)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(b => b.Title.Contains(searchString) || 
                                        (b.Content != null && b.Content.Contains(searchString)) ||
                                        (b.Author != null && b.Author.FullName.Contains(searchString)));
            }

            // Filter by approval status
            if (!string.IsNullOrEmpty(filterApproved))
            {
                if (filterApproved == "approved")
                    query = query.Where(b => b.IsApproved);
                else if (filterApproved == "pending")
                    query = query.Where(b => !b.IsApproved);
            }

            // Get pagination parameters
            var currentPage = PaginationHelper.GetPageNumber(page);
            var currentPageSize = PaginationHelper.GetPageSize(pageSize, 10);
            var totalItems = await query.CountAsync();

            // Pagination
            var blogPosts = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((currentPage - 1) * currentPageSize)
                .Take(currentPageSize)
                .ToListAsync();

            var pagedResult = new PagedResult<BlogPost>(blogPosts, totalItems, currentPage, currentPageSize);

            ViewBag.SearchString = searchString;
            ViewBag.FilterApproved = filterApproved;
            ViewBag.PageSizeOptions = PaginationHelper.GetPageSizeOptions();

            return View(pagedResult);
        }

        // Duyệt bài viết
        [HttpPost]
        public async Task<IActionResult> ApproveBlogPost(int id)
        {
            var blogPost = await _context.BlogPosts
                .Include(b => b.Author)
                .FirstOrDefaultAsync(b => b.Id == id);
            
            if (blogPost != null && !blogPost.IsApproved)
            {
                blogPost.IsApproved = true;  // Cập nhật trạng thái duyệt
                _context.Update(blogPost);
                await _context.SaveChangesAsync();

                // Gửi thông báo cho tác giả bài viết
                if (blogPost.AuthorId != null)
                {
                    var notificationService = HttpContext.RequestServices.GetRequiredService<Services.NotificationService>();
                    await notificationService.NotifyBlogPostApprovedAsync(blogPost.AuthorId, blogPost.Title);
                }

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
