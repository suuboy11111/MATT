using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class SupporterController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    public SupporterController(ApplicationDbContext context, NotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> CreateSupporter()
    {
        var maiAms = await _context.MaiAms.ToListAsync();
        ViewBag.MaiAms = new SelectList(maiAms, "Id", "Name");
        
        var supportTypesList = await _context.SupportTypes.ToListAsync();
        ViewBag.SupportTypes = new SelectList(supportTypesList, "Id", "Name");
        
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupporter(Supporter model, IFormFile ImageFile, int[] supportTypes, int? MaiAmId)
    {
        // Remove ImageFile khỏi ModelState để không ảnh hưởng đến validation
        ModelState.Remove("ImageFile");
        
        // Upload ảnh (xử lý riêng, không phụ thuộc vào ModelState.IsValid)
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
                    var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/supporters");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }

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
            // Set giá trị mặc định thay vì null để tránh lỗi database
            model.ImageUrl = model.ImageUrl ?? "";
        }

        // Đảm bảo ImageUrl không null trước khi lưu
        if (string.IsNullOrEmpty(model.ImageUrl))
        {
            model.ImageUrl = "";
        }

        // Remove các trường không cần validate từ ModelState
        ModelState.Remove("SupportTypes");
        ModelState.Remove("SupporterSupportTypes");
        ModelState.Remove("SupporterMaiAms");
        ModelState.Remove("MaiAm");
        
        // Debug: Log ModelState errors nếu có
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                .Select(x => new { Key = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                .ToList();
            
            // Thêm errors vào TempData để hiển thị
            var errorMessages = errors.SelectMany(e => e.Errors).ToList();
            if (errorMessages.Any())
            {
                TempData["Error"] = string.Join("<br/>", errorMessages);
            }
        }

        // Kiểm tra ModelState sau khi đã xử lý file
        if (ModelState.IsValid)
        {
            model.CreatedDate = DateTime.Now;
            model.UpdatedDate = DateTime.Now;
            model.IsApproved = false;

            _context.Supporters.Add(model);
            await _context.SaveChangesAsync();

            if (supportTypes != null && supportTypes.Length > 0)
            {
                // Lấy danh sách SupportTypeId hợp lệ từ database
                var validSupportTypeIds = await _context.SupportTypes
                    .Select(st => st.Id)
                    .ToListAsync();

                foreach (var typeId in supportTypes)
                {
                    // Chỉ thêm nếu SupportTypeId tồn tại trong database
                    if (validSupportTypeIds.Contains(typeId))
                    {
                        _context.SupporterSupportTypes.Add(new SupporterSupportType
                        {
                            SupporterId = model.Id,
                            SupportTypeId = typeId
                        });
                    }
                }
                
                // Chỉ save nếu có ít nhất 1 SupporterSupportType được thêm
                if (_context.ChangeTracker.Entries<SupporterSupportType>().Any(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added))
                {
                    await _context.SaveChangesAsync();
                }
            }

            if (MaiAmId.HasValue)
            {
                var supporterMaiAm = new SupporterMaiAm
                {
                    SupporterId = model.Id,
                    MaiAmId = MaiAmId.Value
                };

                _context.SupporterMaiAms.Add(supporterMaiAm);
                await _context.SaveChangesAsync();
            }

            // Gửi thông báo cho user nếu đã đăng nhập
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _notificationService.NotifySupporterCreatedAsync(user.Id, model.Name);
                }
            }

            // Gửi thông báo cho tất cả admin
            await _notificationService.NotifyAdminNewSupporterAsync(model.Name, model.Id);

            TempData["Message"] = "Đăng ký người hỗ trợ thành công!";
            return RedirectToAction("Index", "Home");
        }

        // Nếu không hợp lệ thì load lại dropdown và view
        var maiAms = await _context.MaiAms.ToListAsync();
        ViewBag.MaiAms = new SelectList(maiAms, "Id", "Name");

        var supportTypesList = await _context.SupportTypes.ToListAsync();
        ViewBag.SupportTypes = new SelectList(supportTypesList, "Id", "Name");
        
        // Hiển thị lỗi validation
        if (TempData["Error"] == null && !ModelState.IsValid)
        {
            var errorMessages = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            if (errorMessages.Any())
            {
                TempData["Error"] = string.Join("<br/>", errorMessages);
            }
        }

        return View(model);
    }
}
