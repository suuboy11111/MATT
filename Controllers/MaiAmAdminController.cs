using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MaiAmTinhThuong.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class MaiAmAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public MaiAmAdminController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ======= Quản lý Mái ấm =======
        public async Task<IActionResult> Manage()
        {
            var maiAms = await _context.MaiAms.ToListAsync();
            return View("~/Views/Admin/MaiAmManage.cshtml", maiAms);
        }

        // GET: Admin/MaiAmAdmin/Create
        public IActionResult Create()
        {
            return View("~/Views/Admin/Create.cshtml");
        }

        // POST: Admin/MaiAmAdmin/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaiAm maiAm)
        {
            if (ModelState.IsValid)
            {
                maiAm.CreatedDate = System.DateTime.UtcNow;
                maiAm.UpdatedDate = System.DateTime.UtcNow;
                _context.Add(maiAm);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Manage));
            }
            return View("~/Views/Admin/Create.cshtml", maiAm);
        }

        // GET: Admin/MaiAmAdmin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var maiAm = await _context.MaiAms.FindAsync(id);
            if (maiAm == null) return NotFound();

            return View("~/Views/Admin/Edit.cshtml", maiAm);
        }

        // POST: Admin/MaiAmAdmin/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MaiAm maiAm)
        {
            if (id != maiAm.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    maiAm.UpdatedDate = System.DateTime.UtcNow;
                    _context.Update(maiAm);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.MaiAms.Any(e => e.Id == maiAm.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Manage));
            }
            return View("~/Views/Admin/Edit.cshtml", maiAm);
        }

        // GET: Admin/MaiAmAdmin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var maiAm = await _context.MaiAms.FirstOrDefaultAsync(m => m.Id == id);
            if (maiAm == null) return NotFound();

            return View("~/Views/Admin/Delete.cshtml", maiAm);
        }

        // POST: Admin/MaiAmAdmin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var maiAm = await _context.MaiAms.FindAsync(id);
            if (maiAm == null) return NotFound();
            
            _context.MaiAms.Remove(maiAm);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }

        // GET: Admin/MaiAmAdmin/ManageProfiles/5
        public async Task<IActionResult> ManageProfiles(int? id, string searchString, string filterApproved, string filterSupported, string sortBy)
        {
            if (id == null) return NotFound();

            var maiAm = await _context.MaiAms
                .Include(m => m.SupportRequests)
                .Include(m => m.SupporterMaiAms)
                    .ThenInclude(sma => sma.Supporter)
                        .ThenInclude(s => s.SupporterSupportTypes)
                            .ThenInclude(sst => sst.SupportType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maiAm == null) return NotFound();

            // Áp dụng tìm kiếm và lọc trên collection đã load (client-side filtering)
            var supportRequests = maiAm.SupportRequests.AsEnumerable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                supportRequests = supportRequests.Where(sr => 
                    (sr.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (sr.Reason?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (sr.HealthStatus?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (sr.Address?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (sr.PhoneNumber?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Lọc theo trạng thái duyệt
            if (!string.IsNullOrEmpty(filterApproved))
            {
                bool isApproved = filterApproved == "true";
                supportRequests = supportRequests.Where(sr => sr.IsApproved == isApproved);
            }

            // Lọc theo trạng thái hỗ trợ
            if (!string.IsNullOrEmpty(filterSupported))
            {
                if (filterSupported == "reason")
                    supportRequests = supportRequests.Where(sr => sr.IsSupportedReason);
                else if (filterSupported == "health")
                    supportRequests = supportRequests.Where(sr => sr.IsSupportedHealth);
                else if (filterSupported == "general")
                    supportRequests = supportRequests.Where(sr => sr.IsSupported);
            }

            // Sắp xếp
            switch (sortBy)
            {
                case "name":
                    supportRequests = supportRequests.OrderBy(sr => sr.Name);
                    break;
                case "age":
                    supportRequests = supportRequests.OrderByDescending(sr => sr.Age);
                    break;
                case "date":
                    supportRequests = supportRequests.OrderByDescending(sr => sr.CreatedDate);
                    break;
                default:
                    supportRequests = supportRequests.OrderByDescending(sr => sr.CreatedDate);
                    break;
            }

            // Cập nhật lại SupportRequests đã lọc (convert về List)
            maiAm.SupportRequests = supportRequests.ToList();

            ViewBag.MaiAmName = maiAm.Name;
            ViewBag.SearchString = searchString;
            ViewBag.FilterApproved = filterApproved;
            ViewBag.FilterSupported = filterSupported;
            ViewBag.SortBy = sortBy;

            return View("~/Views/Admin/MaiAmManageProfiles.cshtml", maiAm);
        }

        // GET: Admin/MaiAmAdmin/CreateProfile
        public IActionResult CreateProfile(int maiAmId)
        {
            var model = new SupportRequest { MaiAmId = maiAmId };
            return View("~/Views/Admin/CreateProfile.cshtml", model);
        }

        // POST: Admin/MaiAmAdmin/CreateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(SupportRequest model, IFormFile ImageFile)
        {
            // Loại bỏ các lỗi validation không quan trọng (pattern validation từ HTML5)
            ModelState.Remove("ImageFile");
            var keysToCheck = ModelState.Keys.ToList();
            foreach (var key in keysToCheck)
            {
                var modelStateEntry = ModelState[key];
                if (modelStateEntry?.Errors != null)
                {
                    var hasPatternError = modelStateEntry.Errors.Any(e => e.ErrorMessage?.Contains("pattern") == true || e.ErrorMessage?.Contains("format") == true);
                    if (hasPatternError)
                    {
                        modelStateEntry.Errors.Clear();
                    }
                }
            }

            // Xử lý upload ảnh (ngay cả khi ModelState không hoàn toàn valid)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)");
                }
                else if (ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB");
                }
                else
                {
                    try
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        var uploadDir = Path.Combine(_environment.WebRootPath, "images", "profiles");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        var filePath = Path.Combine(uploadDir, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }
                        model.ImageUrl = "/images/profiles/" + uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ImageFile", $"Lỗi khi lưu file: {ex.Message}");
                    }
                }
            }
            else
            {
                model.ImageUrl = "";  // Hoặc giá trị mặc định hợp lệ, tránh null
            }

            // Kiểm tra các field bắt buộc cơ bản
            if (string.IsNullOrWhiteSpace(model.Name) || 
                model.Age <= 0 || 
                string.IsNullOrWhiteSpace(model.Gender) ||
                string.IsNullOrWhiteSpace(model.PhoneNumber) ||
                string.IsNullOrWhiteSpace(model.Reason))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ các thông tin bắt buộc");
            }

            // Nếu các field bắt buộc đã đầy đủ, lưu vào database
            if (ModelState.IsValid || (!string.IsNullOrWhiteSpace(model.Name) && 
                model.Age > 0 && 
                !string.IsNullOrWhiteSpace(model.Gender) &&
                !string.IsNullOrWhiteSpace(model.PhoneNumber) &&
                !string.IsNullOrWhiteSpace(model.Reason)))
            {
                model.CreatedDate = DateTime.UtcNow;
                model.UpdatedDate = DateTime.UtcNow;
                model.IsApproved = true;
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ManageProfiles), new { id = model.MaiAmId });
            }
            return View("~/Views/Admin/CreateProfile.cshtml", model);
        }

        // GET: Admin/MaiAmAdmin/EditProfile/5
        public async Task<IActionResult> EditProfile(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.SupportRequests.FindAsync(id);
            if (request == null) return NotFound();

            return View("~/Views/Admin/EditProfile.cshtml", request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(int id, SupportRequest model, IFormFile ImageFile)
        {
            if (id != model.Id) return NotFound();

            // Loại bỏ các lỗi validation không quan trọng (pattern validation từ HTML5)
            ModelState.Remove("ImageFile");
            var keysToCheck = ModelState.Keys.ToList();
            foreach (var key in keysToCheck)
            {
                var modelStateEntry = ModelState[key];
                if (modelStateEntry?.Errors != null)
                {
                    var hasPatternError = modelStateEntry.Errors.Any(e => e.ErrorMessage?.Contains("pattern") == true || e.ErrorMessage?.Contains("format") == true);
                    if (hasPatternError)
                    {
                        modelStateEntry.Errors.Clear();
                    }
                }
            }

            try
            {
                var existing = await _context.SupportRequests.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                if (existing == null) return NotFound();

                // Xử lý lưu file ảnh mới (ngay cả khi ModelState không hoàn toàn valid)
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)");
                    }
                    // Validate file size (max 5MB)
                    else if (ImageFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB");
                    }
                    else
                    {
                        // File hợp lệ, lưu file
                        try
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                            var uploadDir = Path.Combine(_environment.WebRootPath, "images", "profiles");
                            if (!Directory.Exists(uploadDir))
                            {
                                Directory.CreateDirectory(uploadDir);
                            }

                            var filePath = Path.Combine(uploadDir, uniqueFileName);
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await ImageFile.CopyToAsync(stream);
                            }
                            model.ImageUrl = "/images/profiles/" + uniqueFileName;
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("ImageFile", $"Lỗi khi lưu file: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Giữ nguyên ảnh cũ nếu không upload mới, đảm bảo không null
                    model.ImageUrl = existing.ImageUrl ?? "";
                }

                // Đảm bảo ImageUrl không null trước khi lưu
                if (string.IsNullOrEmpty(model.ImageUrl))
                {
                    model.ImageUrl = "";
                }

                // Kiểm tra các field bắt buộc cơ bản
                if (string.IsNullOrWhiteSpace(model.Name) || 
                    model.Age <= 0 || 
                    string.IsNullOrWhiteSpace(model.Gender) ||
                    string.IsNullOrWhiteSpace(model.PhoneNumber) ||
                    string.IsNullOrWhiteSpace(model.Reason))
                {
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ các thông tin bắt buộc");
                }

                // Nếu các field bắt buộc đã đầy đủ, lưu vào database
                if (ModelState.IsValid || (!string.IsNullOrWhiteSpace(model.Name) && 
                    model.Age > 0 && 
                    !string.IsNullOrWhiteSpace(model.Gender) &&
                    !string.IsNullOrWhiteSpace(model.PhoneNumber) &&
                    !string.IsNullOrWhiteSpace(model.Reason)))
                {
                    model.UpdatedDate = DateTime.UtcNow;
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(ManageProfiles), new { id = model.MaiAmId });
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.SupportRequests.Any(e => e.Id == model.Id)) return NotFound();
                else throw;
            }
            return View("~/Views/Admin/EditProfile.cshtml", model);
        }

        // GET: Admin/MaiAmAdmin/DeleteProfile/5
        public async Task<IActionResult> DeleteProfile(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.SupportRequests.FirstOrDefaultAsync(m => m.Id == id);
            if (request == null) return NotFound();

            return View("~/Views/Admin/DeleteProfile.cshtml", request);
        }

        // POST: Admin/MaiAmAdmin/DeleteProfile/5
        [HttpPost, ActionName("DeleteProfile")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileConfirmed(int id)
        {
            var request = await _context.SupportRequests.FindAsync(id);
            if (request == null) return NotFound();
            
            int maiAmId = request.MaiAmId ?? 0;
            _context.SupportRequests.Remove(request);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(ManageProfiles), new { id = maiAmId });
        }

        // ======= Đánh dấu trạng thái hỗ trợ =======
        [HttpPost]
        public async Task<IActionResult> ToggleSupportStatus(int id, string supportType)
        {
            var request = await _context.SupportRequests.FindAsync(id);
            if (request == null) return NotFound();

            switch (supportType)
            {
                case "Reason":
                    request.IsSupportedReason = !request.IsSupportedReason;
                    break;
                case "Health":
                    request.IsSupportedHealth = !request.IsSupportedHealth;
                    break;
                case "General":
                    request.IsSupported = !request.IsSupported;
                    break;
                default:
                    return BadRequest("Loại hỗ trợ không hợp lệ.");
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        // GET: Admin/MaiAmAdmin/DetailsProfile/5
        [HttpGet]
        public async Task<IActionResult> DetailsProfile(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var request = await _context.SupportRequests
                    .Include(r => r.MaiAm)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (request == null) return NotFound();

                return View("~/Views/Admin/DetailsProfile.cshtml", request);
            }
            catch
            {
                // Log error nếu cần
                return NotFound();
            }
        }
        [HttpPost]
        public async Task<IActionResult> ApproveSupportRequest(int id)
        {
            try
            {
                var request = await _context.SupportRequests.FindAsync(id);
                if (request == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy hồ sơ cần duyệt." });
                }

                if (request.IsApproved)
                {
                    return Json(new { success = true, message = "Hồ sơ này đã được duyệt trước đó." });
                }

                request.IsApproved = true;
                await _context.SaveChangesAsync();

                // Gửi thông báo cho user nếu có user liên kết
                try
                {
                    var notificationService = HttpContext.RequestServices.GetRequiredService<MaiAmTinhThuong.Services.NotificationService>();
                    var userManager = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MaiAmTinhThuong.Models.ApplicationUser>>();
                    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<MaiAmAdminController>>();
                    
                    MaiAmTinhThuong.Models.ApplicationUser user = null;
                    
                    // Bước 1: Tìm user theo phone number (chính xác hoặc chuẩn hóa)
                    if (!string.IsNullOrEmpty(request.PhoneNumber))
                    {
                        var normalizedRequestPhone = request.PhoneNumber?.Replace(" ", "").Replace("+", "").Replace("-", "").Trim();
                        // Chuẩn hóa: nếu bắt đầu bằng 84, đổi thành 0
                        if (!string.IsNullOrEmpty(normalizedRequestPhone) && normalizedRequestPhone.StartsWith("84"))
                        {
                            normalizedRequestPhone = "0" + normalizedRequestPhone.Substring(2);
                        }
                        
                        if (!string.IsNullOrEmpty(normalizedRequestPhone))
                        {
                            var allUsers = await userManager.Users.ToListAsync();
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
                    }
                    
                    // Bước 2: Nếu không tìm thấy, thử tìm theo tên (nếu trùng tên)
                    if (user == null && !string.IsNullOrEmpty(request.Name))
                    {
                        user = await userManager.Users.FirstOrDefaultAsync(u => 
                            u.FullName != null && u.FullName.Trim().Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase));
                    }
                    
                    // Bước 3: Nếu vẫn không tìm thấy, tìm tất cả user có thông báo "chờ duyệt" cho hồ sơ này
                    if (user == null && !string.IsNullOrEmpty(request.Name))
                    {
                        var allNotifications = await _context.Notifications
                            .Where(n => n.Title == "Đăng ký hồ sơ thành công"
                                && n.Message != null
                                && n.Message.Contains(request.Name))
                            .ToListAsync();
                        
                        if (allNotifications.Any())
                        {
                            var userIds = allNotifications.Select(n => n.UserId).Distinct().Where(id => !string.IsNullOrEmpty(id)).ToList();
                            if (userIds.Any())
                            {
                                // Lấy user đầu tiên có thông báo về hồ sơ này
                                user = await userManager.Users.FirstOrDefaultAsync(u => userIds.Contains(u.Id));
                            }
                        }
                    }
                    
                    if (user != null)
                    {
                        logger.LogInformation($"Đã tìm thấy user {user.Id} ({user.Email}) cho hồ sơ {id}, gửi thông báo duyệt");
                        await notificationService.NotifySupportRequestApprovedAsync(user.Id, request.Name ?? "Hồ sơ");
                    }
                    else
                    {
                        logger.LogWarning($"Không tìm thấy user cho hồ sơ {id} (Phone: {request.PhoneNumber}, Name: {request.Name}). Không thể gửi thông báo.");
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không làm gián đoạn quá trình duyệt
                    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<MaiAmAdminController>>();
                    logger.LogError(ex, $"Lỗi khi gửi thông báo duyệt cho hồ sơ {id}");
                }

                return Json(new { success = true, message = "Hồ sơ đã được duyệt thành công!" });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<MaiAmAdminController>>();
                logger.LogError(ex, $"Lỗi khi duyệt hồ sơ {id}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi duyệt hồ sơ. Vui lòng thử lại sau." });
            }
        }
        public async Task<IActionResult> ManageSupporters(int? id, string searchString, string filterApproved, string filterSupportType, string sortBy)
        {
            if (id == null) return NotFound();

            // Lọc chỉ lấy các mái ấm có người hỗ trợ hợp lệ
            var maiAm = await _context.MaiAms
                .Include(m => m.SupporterMaiAms)
                    .ThenInclude(sma => sma.Supporter)
                        .ThenInclude(s => s.SupporterSupportTypes)
                            .ThenInclude(sst => sst.SupportType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maiAm == null) return NotFound();

            // Áp dụng tìm kiếm và lọc trên collection đã load (client-side filtering)
            var supporters = maiAm.SupporterMaiAms.Select(sma => sma.Supporter).Where(s => s != null).AsEnumerable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                supporters = supporters.Where(s => 
                    (s.Name?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Address?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.PhoneNumber?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            // Lọc theo trạng thái duyệt
            if (!string.IsNullOrEmpty(filterApproved))
            {
                bool isApproved = filterApproved == "true";
                supporters = supporters.Where(s => s.IsApproved == isApproved);
            }

            // Lọc theo loại hỗ trợ
            if (!string.IsNullOrEmpty(filterSupportType))
            {
                supporters = supporters.Where(s => s.SupporterSupportTypes?.Any(sst => sst.SupportType?.Name == filterSupportType) ?? false);
            }

            // Sắp xếp
            switch (sortBy)
            {
                case "name":
                    supporters = supporters.OrderBy(s => s.Name);
                    break;
                case "age":
                    supporters = supporters.OrderByDescending(s => s.Age);
                    break;
                case "date":
                    supporters = supporters.OrderByDescending(s => s.CreatedDate);
                    break;
                default:
                    supporters = supporters.OrderByDescending(s => s.CreatedDate);
                    break;
            }

            // Cập nhật lại SupporterMaiAms với supporters đã lọc
            var supporterIds = supporters.Select(s => s.Id).ToList();
            maiAm.SupporterMaiAms = maiAm.SupporterMaiAms.Where(sma => supporterIds.Contains(sma.Supporter?.Id ?? 0)).ToList();

            ViewBag.SearchString = searchString;
            ViewBag.FilterApproved = filterApproved;
            ViewBag.FilterSupportType = filterSupportType;
            ViewBag.SortBy = sortBy;
            ViewBag.SupportTypes = await _context.SupportTypes.ToListAsync();

            return View("~/Views/Admin/ManageSupporters.cshtml", maiAm);
        }
        // Xem chi tiết người hỗ trợ
        [HttpGet]
        public async Task<IActionResult> DetailsSupporter(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                var supporter = await _context.Supporters
                    .Include(s => s.SupporterSupportTypes)
                        .ThenInclude(sst => sst.SupportType)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supporter == null) return NotFound();

                return View("~/Views/Admin/DetailsSupporter.cshtml", supporter);
            }
            catch
            {
                return NotFound();
            }
        }

        // GET: Edit người hỗ trợ
        public async Task<IActionResult> EditSupporter(int? id)
        {
            if (id == null) return NotFound();

            var supporter = await _context.Supporters
                .Include(s => s.SupporterSupportTypes)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supporter == null) return NotFound();

            // Lấy danh sách mái ấm và loại hỗ trợ để dropdown/checkbox trong view
            ViewBag.MaiAms = new SelectList(await _context.MaiAms.ToListAsync(), "Id", "Name");
            ViewBag.SupportTypes = await _context.SupportTypes.ToListAsync();

            return View("~/Views/Admin/EditSupporter.cshtml", supporter);
        }
        // POST: Edit người hỗ trợ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupporter(int id, Supporter model, IFormFile ImageFile, int[] selectedSupportTypes)
        {
            if (id != model.Id) return NotFound();

            // Loại bỏ các lỗi validation không quan trọng (pattern validation từ HTML5)
            ModelState.Remove("ImageFile");
            var keysToCheck = ModelState.Keys.ToList();
            foreach (var key in keysToCheck)
            {
                var modelStateEntry = ModelState[key];
                if (modelStateEntry?.Errors != null)
                {
                    var hasPatternError = modelStateEntry.Errors.Any(e => e.ErrorMessage?.Contains("pattern") == true || e.ErrorMessage?.Contains("format") == true);
                    if (hasPatternError)
                    {
                        modelStateEntry.Errors.Clear();
                    }
                }
            }

            // Lấy thông tin supporter hiện tại để giữ ảnh cũ nếu không upload mới
            var existing = await _context.Supporters.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
            if (existing == null) return NotFound();

            // Xử lý upload ảnh nếu có (ngay cả khi ModelState không hoàn toàn valid)
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)");
                }
                else if (ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Kích thước file không được vượt quá 5MB");
                }
                else
                {
                    try
                    {
                        var uploadDir = Path.Combine(_environment.WebRootPath, "images", "supporters");
                        if (!Directory.Exists(uploadDir))
                            Directory.CreateDirectory(uploadDir);

                        var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        var filePath = Path.Combine(uploadDir, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        model.ImageUrl = "/images/supporters/" + uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("ImageFile", $"Lỗi khi lưu file: {ex.Message}");
                    }
                }
            }
            else
            {
                // Giữ nguyên ảnh cũ nếu không upload mới, đảm bảo không null
                model.ImageUrl = existing.ImageUrl ?? "";
            }

            // Đảm bảo ImageUrl không null trước khi lưu
            if (string.IsNullOrEmpty(model.ImageUrl))
            {
                model.ImageUrl = "";
            }

            // Kiểm tra các field bắt buộc cơ bản
            if (string.IsNullOrWhiteSpace(model.Name) || 
                model.Age <= 0 || 
                string.IsNullOrWhiteSpace(model.Gender) ||
                string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ các thông tin bắt buộc");
            }

            // Nếu các field bắt buộc đã đầy đủ, lưu vào database
            if (ModelState.IsValid || (!string.IsNullOrWhiteSpace(model.Name) && 
                model.Age > 0 && 
                !string.IsNullOrWhiteSpace(model.Gender) &&
                !string.IsNullOrWhiteSpace(model.PhoneNumber)))
            {
                try
                {
                    // Cập nhật supporter
                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    // Cập nhật lại loại hỗ trợ
                    // Xóa các loại hỗ trợ cũ
                    var oldSupportTypes = _context.SupporterSupportTypes.Where(sst => sst.SupporterId == model.Id);
                    _context.SupporterSupportTypes.RemoveRange(oldSupportTypes);
                    await _context.SaveChangesAsync();

                    // Thêm loại hỗ trợ mới
                    if (selectedSupportTypes != null)
                    {
                        foreach (var typeId in selectedSupportTypes)
                        {
                            _context.SupporterSupportTypes.Add(new SupporterSupportType
                            {
                                SupporterId = model.Id,
                                SupportTypeId = typeId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Supporters.Any(e => e.Id == model.Id))
                        return NotFound();
                    else
                        throw;
                }

                return RedirectToAction("ManageSupporters", new { id = model.MaiAmId });
            }

            ViewBag.MaiAms = new SelectList(await _context.MaiAms.ToListAsync(), "Id", "Name");
            ViewBag.SupportTypes = await _context.SupportTypes.ToListAsync();
            return View(model);
        }

        // GET: Xác nhận xóa người hỗ trợ
        public async Task<IActionResult> DeleteSupporter(int? id)
        {
            if (id == null) return NotFound();

            var supporter = await _context.Supporters
                .Include(s => s.SupporterSupportTypes)
                    .ThenInclude(sst => sst.SupportType)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supporter == null) return NotFound();

            return View("~/Views/Admin/DeleteSupporter.cshtml", supporter);
        }

        // POST: Xóa người hỗ trợ
        [HttpPost, ActionName("DeleteSupporter")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupporterConfirmed(int id)
        {
            var supporter = await _context.Supporters.FindAsync(id);
            int maiAmId = 0;
            
            if (supporter != null)
            {
                maiAmId = supporter.MaiAmId ?? 0;
                _context.Supporters.Remove(supporter);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageSupporters", new { id = maiAmId });
        }
        [HttpPost]
        public async Task<IActionResult> ApproveSupporter(int supporterId)
        {
            try
            {
                var supporter = await _context.Supporters.FindAsync(supporterId);
                if (supporter == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người hỗ trợ cần duyệt." });
                }

                if (supporter.IsApproved)
                {
                    return Json(new { success = true, message = "Người hỗ trợ này đã được duyệt trước đó." });
                }

                supporter.IsApproved = true;
                await _context.SaveChangesAsync();

                // Gửi thông báo cho user nếu có user liên kết
                try
                {
                    var notificationService = HttpContext.RequestServices.GetRequiredService<MaiAmTinhThuong.Services.NotificationService>();
                    var userManager = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MaiAmTinhThuong.Models.ApplicationUser>>();
                    
                    // Tìm user theo phone number (chính xác hoặc chứa)
                    var user = await userManager.Users.FirstOrDefaultAsync(u => 
                        (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber == supporter.PhoneNumber) ||
                        (!string.IsNullOrEmpty(u.PhoneNumber2) && u.PhoneNumber2 == supporter.PhoneNumber) ||
                        (!string.IsNullOrEmpty(supporter.PhoneNumber) && !string.IsNullOrEmpty(u.Email) && u.Email.Contains(supporter.PhoneNumber)));
                    
                    // Nếu không tìm thấy, thử tìm theo tên
                    if (user == null && !string.IsNullOrEmpty(supporter.PhoneNumber))
                    {
                        user = await userManager.Users.FirstOrDefaultAsync(u => 
                            u.FullName == supporter.Name && 
                            (!string.IsNullOrEmpty(u.PhoneNumber) && (u.PhoneNumber.Contains(supporter.PhoneNumber) || supporter.PhoneNumber.Contains(u.PhoneNumber))));
                    }
                    
                    if (user != null)
                    {
                        await notificationService.NotifySupporterApprovedAsync(user.Id, supporter.Name ?? "Người hỗ trợ");
                    }
                    else if (!string.IsNullOrEmpty(supporter.PhoneNumber) && supporter.PhoneNumber.Length >= 4)
                    {
                        // Nếu không tìm thấy user, thử tìm các user có phone number tương tự
                        var phoneSuffix = supporter.PhoneNumber.Substring(Math.Max(0, supporter.PhoneNumber.Length - 4));
                        var similarUsers = await userManager.Users
                            .Where(u => (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Contains(phoneSuffix)) ||
                                       (!string.IsNullOrEmpty(u.PhoneNumber2) && u.PhoneNumber2.Contains(phoneSuffix)))
                            .ToListAsync();
                        
                        foreach (var similarUser in similarUsers)
                        {
                            await notificationService.NotifySupporterApprovedAsync(similarUser.Id, supporter.Name ?? "Người hỗ trợ");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không làm gián đoạn quá trình duyệt
                    var logger = HttpContext.RequestServices.GetRequiredService<ILogger<MaiAmAdminController>>();
                    logger.LogWarning(ex, $"Không thể gửi thông báo khi duyệt người hỗ trợ {supporterId}");
                }

                return Json(new { success = true, message = "Người hỗ trợ đã được duyệt thành công!" });
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<MaiAmAdminController>>();
                logger.LogError(ex, $"Lỗi khi duyệt người hỗ trợ {supporterId}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi duyệt người hỗ trợ. Vui lòng thử lại sau." });
            }
        }
    }
}
