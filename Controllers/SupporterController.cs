using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;

public class SupporterController : Controller
{
    private readonly ApplicationDbContext _context;

    public SupporterController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult CreateSupporter()
    {
        var maiAms = _context.MaiAms.ToList();
        ViewBag.MaiAms = new SelectList(maiAms, "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupporter(Supporter model, IFormFile ImageFile, int[] supportTypes, int? MaiAmId)
    {
        if (ModelState.IsValid)
        {
            // Upload ảnh
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/supporters");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

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
                model.ImageUrl = "";
            }

            model.CreatedDate = DateTime.Now;
            model.UpdatedDate = DateTime.Now;
            model.IsApproved = false;

            _context.Supporters.Add(model);
            await _context.SaveChangesAsync();

            if (supportTypes != null && supportTypes.Length > 0)
            {
                foreach (var typeId in supportTypes)
                {
                    _context.SupporterSupportTypes.Add(new SupporterSupportType
                    {
                        SupporterId = model.Id,
                        SupportTypeId = typeId
                    });
                }
                await _context.SaveChangesAsync();
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

            TempData["Message"] = "Đăng ký người hỗ trợ thành công!";
            return RedirectToAction("Index", "Home");
        }

        // Nếu không hợp lệ thì load lại dropdown và view
        var maiAms = _context.MaiAms.ToList();
        ViewBag.MaiAms = new SelectList(maiAms, "Id", "Name");

        var supportTypesList = _context.SupportTypes.ToList();
        ViewBag.SupportTypes = new SelectList(supportTypesList, "Id", "Name");

        return View(model);
    }
}
