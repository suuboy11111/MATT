using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using MaiAmTinhThuong.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveSupporter(int id)
        {
            try
            {
                var supporter = await _context.Supporters
                    .Include(s => s.MaiAm)
                    .FirstOrDefaultAsync(s => s.Id == id);
                
                if (supporter == null)
                {
                    TempData["Error"] = "Không tìm thấy người hỗ trợ cần duyệt.";
                    return RedirectToAction("ManageSupporters");
                }

                if (supporter.IsApproved)
                {
                    TempData["Message"] = "Người hỗ trợ này đã được duyệt trước đó.";
                    return RedirectToAction("ManageSupporters");
                }

                supporter.IsApproved = true;
                supporter.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                // Gửi thông báo cho user nếu có user liên kết
                try
                {
                    var notificationService = HttpContext.RequestServices.GetRequiredService<Services.NotificationService>();
                    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                    
                    // Tìm user theo phone number
                    var normalizedPhone = supporter.PhoneNumber?.Replace(" ", "").Replace("+", "").Replace("-", "").Trim();
                    if (!string.IsNullOrEmpty(normalizedPhone) && normalizedPhone.StartsWith("84"))
                    {
                        normalizedPhone = "0" + normalizedPhone.Substring(2);
                    }
                    
                    ApplicationUser user = null;
                    if (!string.IsNullOrEmpty(normalizedPhone))
                    {
                        var allUsers = await _userManager.Users.ToListAsync();
                        user = allUsers.FirstOrDefault(u => 
                        {
                            var userPhone1 = u.PhoneNumber?.Replace(" ", "").Replace("+", "").Replace("-", "").Trim();
                            var userPhone2 = u.PhoneNumber2?.Replace(" ", "").Replace("+", "").Replace("-", "").Trim();
                            
                            if (!string.IsNullOrEmpty(userPhone1) && userPhone1.StartsWith("84"))
                                userPhone1 = "0" + userPhone1.Substring(2);
                            if (!string.IsNullOrEmpty(userPhone2) && userPhone2.StartsWith("84"))
                                userPhone2 = "0" + userPhone2.Substring(2);
                            
                            return userPhone1 == normalizedPhone || userPhone2 == normalizedPhone;
                        });
                    }
                    
                    if (user != null)
                    {
                        logger.LogInformation($"Đã tìm thấy user {user.Id} ({user.Email}) cho supporter {id}, gửi thông báo duyệt");
                        await notificationService.NotifySupporterApprovedAsync(user.Id, supporter.Name ?? "Người hỗ trợ");
                    }
                }
                catch (Exception ex)
                {
                    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                    logger.LogError(ex, $"Lỗi khi gửi thông báo duyệt cho supporter {id}");
                }
                
                TempData["Message"] = "Người hỗ trợ đã được duyệt thành công!";
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                logger.LogError(ex, $"Lỗi khi duyệt supporter {id}");
                TempData["Error"] = "Có lỗi xảy ra khi duyệt người hỗ trợ. Vui lòng thử lại sau.";
            }
            
            return RedirectToAction("ManageSupporters");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var request = await _context.SupportRequests
                    .Include(r => r.MaiAm)
                    .FirstOrDefaultAsync(r => r.Id == id);
                
                if (request == null)
                {
                    TempData["Error"] = "Không tìm thấy hồ sơ cần duyệt.";
                    return RedirectToAction("ManageSupportRequests");
                }

                if (request.IsApproved)
                {
                    TempData["Message"] = "Hồ sơ này đã được duyệt trước đó.";
                    return RedirectToAction("ManageSupportRequests");
                }

                request.IsApproved = true;
                request.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                // Gửi thông báo cho user nếu có user liên kết
                try
                {
                    var notificationService = HttpContext.RequestServices.GetRequiredService<Services.NotificationService>();
                    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                    
                    // Chuẩn hóa phone number để so sánh (loại bỏ khoảng trắng, dấu +, dấu -, và chuẩn hóa format)
                    var normalizedRequestPhone = request.PhoneNumber?.Replace(" ", "").Replace("+", "").Replace("-", "").Trim();
                    // Chuẩn hóa thêm: nếu bắt đầu bằng 0, giữ nguyên; nếu bắt đầu bằng 84, đổi thành 0
                    if (!string.IsNullOrEmpty(normalizedRequestPhone) && normalizedRequestPhone.StartsWith("84"))
                    {
                        normalizedRequestPhone = "0" + normalizedRequestPhone.Substring(2);
                    }
                    
                    ApplicationUser user = null;
                    
                    // Tìm user theo phone number (chính xác hoặc chuẩn hóa)
                    if (!string.IsNullOrEmpty(normalizedRequestPhone))
                    {
                        var allUsers = await _userManager.Users.ToListAsync();
                        user = allUsers.FirstOrDefault(u => 
                        {
                            var normalizedUserPhone1 = u.PhoneNumber?.Replace(" ", "").Replace("+", "").Replace("-", "").Trim();
                            var normalizedUserPhone2 = u.PhoneNumber2?.Replace(" ", "").Replace("+", "").Replace("-", "").Trim();
                            
                            // Chuẩn hóa user phone numbers
                            if (!string.IsNullOrEmpty(normalizedUserPhone1) && normalizedUserPhone1.StartsWith("84"))
                            {
                                normalizedUserPhone1 = "0" + normalizedUserPhone1.Substring(2);
                            }
                            if (!string.IsNullOrEmpty(normalizedUserPhone2) && normalizedUserPhone2.StartsWith("84"))
                            {
                                normalizedUserPhone2 = "0" + normalizedUserPhone2.Substring(2);
                            }
                            
                            return normalizedUserPhone1 == normalizedRequestPhone || normalizedUserPhone2 == normalizedRequestPhone;
                        });
                    }
                    
                    // Nếu không tìm thấy, thử tìm theo tên (nếu trùng tên)
                    if (user == null && !string.IsNullOrEmpty(request.Name))
                    {
                        user = await _userManager.Users.FirstOrDefaultAsync(u => 
                            u.FullName != null && u.FullName.Trim().Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                    }
                    
                    if (user != null)
                    {
                        logger.LogInformation($"Đã tìm thấy user {user.Id} ({user.Email}) cho hồ sơ {id}, gửi thông báo duyệt");
                        await notificationService.NotifySupportRequestApprovedAsync(user.Id, request.Name ?? "Hồ sơ");
                    }
                    else
                    {
                        logger.LogWarning($"Không tìm thấy user cho hồ sơ {id} (Phone: {request.PhoneNumber}, Name: {request.Name})");
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không làm gián đoạn quá trình duyệt
                    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                    logger.LogError(ex, $"Lỗi khi gửi thông báo duyệt cho hồ sơ {id}");
                }
                
                TempData["Message"] = "Hồ sơ đã được duyệt!";
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
                logger.LogError(ex, $"Lỗi khi duyệt hồ sơ {id}");
                TempData["Error"] = "Có lỗi xảy ra khi duyệt hồ sơ. Vui lòng thử lại sau.";
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveBlogPost(int id)
        {
            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>();
            
            try
            {
                logger.LogInformation($"Bắt đầu duyệt bài viết {id}");
                
                var blogPost = await _context.BlogPosts
                    .Include(b => b.Author)
                    .FirstOrDefaultAsync(b => b.Id == id);
                
                if (blogPost == null)
                {
                    logger.LogWarning($"Không tìm thấy bài viết với ID {id}");
                    TempData["Error"] = "Không tìm thấy bài viết cần duyệt.";
                    return RedirectToAction("ManageBlogPosts");
                }

                if (blogPost.IsApproved)
                {
                    logger.LogInformation($"Bài viết {id} đã được duyệt trước đó");
                    TempData["Message"] = "Bài viết này đã được duyệt trước đó.";
                    return RedirectToAction("ManageBlogPosts");
                }

                logger.LogInformation($"Đang cập nhật trạng thái duyệt cho bài viết {id}");
                
                // Đảm bảo CreatedAt là UTC trước khi update
                if (blogPost.CreatedAt.Kind != DateTimeKind.Utc)
                {
                    blogPost.CreatedAt = blogPost.CreatedAt.Kind == DateTimeKind.Unspecified 
                        ? DateTime.SpecifyKind(blogPost.CreatedAt, DateTimeKind.Utc)
                        : blogPost.CreatedAt.ToUniversalTime();
                }
                
                blogPost.IsApproved = true;  // Cập nhật trạng thái duyệt
                _context.Update(blogPost);
                
                logger.LogInformation($"Đang lưu thay đổi vào database cho bài viết {id}");
                await _context.SaveChangesAsync();
                logger.LogInformation($"Đã lưu thành công bài viết {id}");

                // Gửi thông báo cho tác giả bài viết
                if (blogPost.AuthorId != null)
                {
                    try
                    {
                        logger.LogInformation($"Đang gửi thông báo cho tác giả {blogPost.AuthorId}");
                        var notificationService = HttpContext.RequestServices.GetRequiredService<Services.NotificationService>();
                        await notificationService.NotifyBlogPostApprovedAsync(blogPost.AuthorId, blogPost.Title ?? "Bài viết");
                        logger.LogInformation($"Đã gửi thông báo thành công cho tác giả {blogPost.AuthorId}");
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng không làm gián đoạn quá trình duyệt
                        logger.LogWarning(ex, $"Không thể gửi thông báo khi duyệt bài viết {id}. Lỗi: {ex.Message}");
                    }
                }
                else
                {
                    logger.LogWarning($"Bài viết {id} không có AuthorId");
                }

                logger.LogInformation($"Duyệt bài viết {id} thành công");
                TempData["Message"] = "Bài viết đã được duyệt thành công!";
            }
            catch (DbUpdateException dbEx)
            {
                logger.LogError(dbEx, $"Lỗi database khi duyệt bài viết {id}. Chi tiết: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    logger.LogError($"Inner exception: {dbEx.InnerException.Message}");
                }
                TempData["Error"] = $"Có lỗi xảy ra khi duyệt bài viết: {dbEx.Message}. Vui lòng thử lại sau.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Lỗi không xác định khi duyệt bài viết {id}. Chi tiết: {ex.Message}");
                if (ex.InnerException != null)
                {
                    logger.LogError($"Inner exception: {ex.InnerException.Message}");
                }
                TempData["Error"] = $"Có lỗi xảy ra khi duyệt bài viết: {ex.Message}. Vui lòng thử lại sau.";
            }
            
            return RedirectToAction("ManageBlogPosts");
        }

        // Xóa bài viết
        [HttpPost]
        [ValidateAntiForgeryToken]
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
