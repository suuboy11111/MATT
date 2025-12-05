using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MaiAmTinhThuong.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class MaiAmAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaiAmAdminController(ApplicationDbContext context)
        {
            _context = context;
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
                maiAm.CreatedDate = System.DateTime.Now;
                maiAm.UpdatedDate = System.DateTime.Now;
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
                    maiAm.UpdatedDate = System.DateTime.Now;
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

            // Xử lý lưu ảnh (ngay cả khi ModelState không hoàn toàn valid)
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
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
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
                model.CreatedDate = DateTime.Now;
                model.UpdatedDate = DateTime.Now;
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
                            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles");
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
                    model.UpdatedDate = DateTime.Now;
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
            var request = await _context.SupportRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.IsApproved = true;
            await _context.SaveChangesAsync();

            // Gửi thông báo cho user nếu có user liên kết
            var notificationService = HttpContext.RequestServices.GetRequiredService<MaiAmTinhThuong.Services.NotificationService>();
            var userManager = HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<MaiAmTinhThuong.Models.ApplicationUser>>();
            
            // Tìm user theo phone number (chính xác hoặc chứa)
            var user = await userManager.Users.FirstOrDefaultAsync(u => 
                (!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber == request.PhoneNumber) ||
                (!string.IsNullOrEmpty(u.PhoneNumber2) && u.PhoneNumber2 == request.PhoneNumber) ||
                (!string.IsNullOrEmpty(request.PhoneNumber) && !string.IsNullOrEmpty(u.Email) && u.Email.Contains(request.PhoneNumber)));
            
            // Nếu không tìm thấy, thử tìm theo tên
            if (user == null && !string.IsNullOrEmpty(request.PhoneNumber))
            {
                user = await userManager.Users.FirstOrDefaultAsync(u => 
                    u.FullName == request.Name && 
                    (!string.IsNullOrEmpty(u.PhoneNumber) && (u.PhoneNumber.Contains(request.PhoneNumber) || request.PhoneNumber.Contains(u.PhoneNumber))));
            }
            
            if (user != null)
            {
                await notificationService.NotifySupportRequestApprovedAsync(user.Id, request.Name);
            }
            else
            {
                // Nếu không tìm thấy user, thử tìm các user có phone number tương tự
                var similarUsers = await userManager.Users
                    .Where(u => !string.IsNullOrEmpty(request.PhoneNumber) && 
                               ((!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Contains(request.PhoneNumber.Substring(Math.Max(0, request.PhoneNumber.Length - 4)))) ||
                                (!string.IsNullOrEmpty(u.PhoneNumber2) && u.PhoneNumber2.Contains(request.PhoneNumber.Substring(Math.Max(0, request.PhoneNumber.Length - 4))))))
                    .ToListAsync();
                
                foreach (var similarUser in similarUsers)
                {
                    await notificationService.NotifySupportRequestApprovedAsync(similarUser.Id, request.Name);
                }
            }

            return Json(new { success = true });
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
            ViewBag.SupportTypes = new SelectList(await _context.SupportTypes.ToListAsync(), "Id", "Name");

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
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/supporters");
                if (!Directory.Exists(uploadDir))
                    Directory.CreateDirectory(uploadDir);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                var filePath = Path.Combine(uploadDir, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                model.ImageUrl = "/images/supporters/" + uniqueFileName;
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
            ViewBag.SupportTypes = new SelectList(await _context.SupportTypes.ToListAsync(), "Id", "Name");
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
            var supporter = await _context.Supporters.FindAsync(supporterId);
            if (supporter == null) return NotFound();

            supporter.IsApproved = true;
            await _context.SaveChangesAsync();

            // Gửi thông báo cho user nếu có user liên kết
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
                await notificationService.NotifySupporterApprovedAsync(user.Id, supporter.Name);
            }
            else
            {
                // Nếu không tìm thấy user, thử tìm các user có phone number tương tự
                var similarUsers = await userManager.Users
                    .Where(u => !string.IsNullOrEmpty(supporter.PhoneNumber) && 
                               ((!string.IsNullOrEmpty(u.PhoneNumber) && u.PhoneNumber.Contains(supporter.PhoneNumber.Substring(Math.Max(0, supporter.PhoneNumber.Length - 4)))) ||
                                (!string.IsNullOrEmpty(u.PhoneNumber2) && u.PhoneNumber2.Contains(supporter.PhoneNumber.Substring(Math.Max(0, supporter.PhoneNumber.Length - 4))))))
                    .ToListAsync();
                
                foreach (var similarUser in similarUsers)
                {
                    await notificationService.NotifySupporterApprovedAsync(similarUser.Id, supporter.Name);
                }
            }

            return Json(new { success = true });
        }
    }
}
