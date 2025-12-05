using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using System.IO;

public class SupportRequestController : Controller
{
    private readonly ApplicationDbContext _context;

    public SupportRequestController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult CreateRequest()
    {
        var maiAms = _context.MaiAms.ToList();
        ViewBag.MaiAms = new SelectList(maiAms, "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(SupportRequest model, IFormFile ImageFile)
    {
        if (ModelState.IsValid)
        {
            // Xử lý lưu ảnh nếu có
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Lưu ảnh vào thư mục và tạo đường dẫn
                var fileName = Path.GetFileName(ImageFile.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles", fileName);

                // Kiểm tra xem thư mục đã tồn tại chưa, nếu không thì tạo
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Lưu ảnh vào thư mục
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(stream);
                }

                // Lưu đường dẫn ảnh vào model
                model.ImageUrl = "/images/profiles/" + fileName;
            }
            else
            {
                model.ImageUrl = "";  // Nếu không có ảnh, để null hoặc giá trị mặc định
            }

            // Cập nhật các trường khác và lưu vào cơ sở dữ liệu
            model.CreatedDate = DateTime.Now;
            model.UpdatedDate = DateTime.Now;
            model.IsApproved = false;

            // Lưu hồ sơ người cần hỗ trợ vào cơ sở dữ liệu
            _context.SupportRequests.Add(model);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Đăng ký hồ sơ cần hỗ trợ thành công!";
            return RedirectToAction("Index", "Home");
        }

        // Nếu có lỗi, trả về lại view
        var maiAms = _context.MaiAms.ToList();
        ViewBag.MaiAms = new SelectList(maiAms, "Id", "Name");
        return View(model);
    }

}
