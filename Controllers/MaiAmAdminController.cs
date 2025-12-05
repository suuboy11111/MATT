using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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
            _context.MaiAms.Remove(maiAm);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Manage));
        }

        // GET: Admin/MaiAmAdmin/ManageProfiles/5
        public async Task<IActionResult> ManageProfiles(int? id)
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

            ViewBag.MaiAmName = maiAm.Name;

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
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    // Lưu ảnh vào thư mục, tạo đường dẫn, gán vào model.ImageUrl
                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }
                    model.ImageUrl = "/images/profiles/" + fileName;
                }
                else
                {
                    model.ImageUrl = "";  // Hoặc giá trị mặc định hợp lệ, tránh null
                }

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

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.SupportRequests.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
                    if (existing == null) return NotFound();

                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Xử lý lưu file ảnh mới
                        var fileName = Path.GetFileName(ImageFile.FileName);
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/profiles", fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }
                        model.ImageUrl = "/images/profiles/" + fileName;
                    }
                    else
                    {
                        // Giữ nguyên ảnh cũ nếu không upload mới
                        model.ImageUrl = existing.ImageUrl;
                    }

                    model.UpdatedDate = DateTime.Now;
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(ManageProfiles), new { id = model.MaiAmId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.SupportRequests.Any(e => e.Id == model.Id)) return NotFound();
                    else throw;
                }
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
        public async Task<IActionResult> DetailsProfile(int? id)
        {
            if (id == null) return NotFound();

            var request = await _context.SupportRequests
                .Include(r => r.MaiAm)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (request == null) return NotFound();

            return View("~/Views/Admin/DetailsProfile.cshtml", request);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveSupportRequest(int id)
        {
            var request = await _context.SupportRequests.FindAsync(id);
            if (request == null) return NotFound();

            request.IsApproved = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        public async Task<IActionResult> ManageSupporters(int? id)
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

            return View("~/Views/Admin/ManageSupporters.cshtml", maiAm);
        }
        // Xem chi tiết người hỗ trợ
        public async Task<IActionResult> DetailsSupporter(int? id)
        {
            if (id == null) return NotFound();

            var supporter = await _context.Supporters
                .Include(s => s.SupporterSupportTypes)
                    .ThenInclude(sst => sst.SupportType)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (supporter == null) return NotFound();

            return View("~/Views/Admin/DetailsSupporter.cshtml", supporter);
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
            ViewBag.MaiAms = new SelectList(_context.MaiAms.ToList(), "Id", "Name");
            ViewBag.SupportTypes = new SelectList(_context.SupportTypes.ToList(), "Id", "Name");

            return View("~/Views/Admin/EditSupporter.cshtml", supporter);
        }
        // POST: Edit người hỗ trợ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupporter(int id, Supporter model, IFormFile ImageFile, int[] selectedSupportTypes)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh nếu có
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

            ViewBag.MaiAms = new SelectList(_context.MaiAms.ToList(), "Id", "Name");
            ViewBag.SupportTypes = new SelectList(_context.SupportTypes.ToList(), "Id", "Name");
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
            if (supporter != null)
            {
                _context.Supporters.Remove(supporter);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("ManageSupporters", new { id = supporter.MaiAmId });
        }
        [HttpPost]
        public async Task<IActionResult> ApproveSupporter(int supporterId)
        {
            var supporter = await _context.Supporters.FindAsync(supporterId);
            if (supporter == null) return NotFound();

            supporter.IsApproved = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
