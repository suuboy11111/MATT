using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;

public class SupportRequestController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly NotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IImageUploadService _imageUploadService;

    public SupportRequestController(ApplicationDbContext context, NotificationService notificationService, UserManager<ApplicationUser> userManager, IImageUploadService imageUploadService)
    {
        _context = context;
        _notificationService = notificationService;
        _userManager = userManager;
        _imageUploadService = imageUploadService;
    }

    [HttpGet]
    public async Task<IActionResult> CreateRequest()
    {
        var maiAms = await _context.MaiAms.ToListAsync();
        ViewBag.MaiAms = new SelectList(maiAms, "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(SupportRequest model, IFormFile ImageFile)
    {
        // Remove ImageFile khỏi ModelState để không ảnh hưởng đến validation
        ModelState.Remove("ImageFile");
        
        // Xử lý lưu ảnh nếu có (xử lý riêng, không phụ thuộc vào ModelState.IsValid)
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
                // File hợp lệ, upload lên Cloudinary
                try
                {
                    var imageUrl = await _imageUploadService.UploadImageAsync(ImageFile, "support-requests");
                    model.ImageUrl = imageUrl;
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("ImageFile", ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("ImageFile", $"Lỗi khi upload ảnh: {ex.Message}");
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

        // Kiểm tra ModelState sau khi đã xử lý file
        if (ModelState.IsValid)
        {
            // Cập nhật các trường khác và lưu vào cơ sở dữ liệu
            model.CreatedDate = DateTime.UtcNow;
            model.UpdatedDate = DateTime.UtcNow;
            model.IsApproved = false;

            // Lưu hồ sơ người cần hỗ trợ vào cơ sở dữ liệu
            _context.SupportRequests.Add(model);
            await _context.SaveChangesAsync();

            // Gửi thông báo cho user nếu đã đăng nhập
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    await _notificationService.NotifySupportRequestCreatedAsync(user.Id, model.Name);
                }
            }

            // Gửi thông báo cho tất cả admin
            await _notificationService.NotifyAdminNewSupportRequestAsync(model.Name, model.Id);

            TempData["Message"] = "Đăng ký hồ sơ cần hỗ trợ thành công!";
            return RedirectToAction("Index", "Home");
        }

        // Nếu có lỗi, trả về lại view
        var maiAms = await _context.MaiAms.ToListAsync();
        ViewBag.MaiAms = new SelectList(maiAms, "Id", "Name");
        return View(model);
    }

}
