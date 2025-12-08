using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

using Microsoft.AspNetCore.Authorization;

namespace MaiAmTinhThuong.Controllers
{
    public class VinhDanhsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VinhDanhsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: VinhDanhs - Ai cũng xem được
        //[HttpGet("")]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString)
        {
            var danhSach = from v in _context.VinhDanhs
                           select v;

            if (!String.IsNullOrEmpty(searchString))
            {
                danhSach = danhSach.Where(v => v.HoTen.Contains(searchString));
            }

            return View(await danhSach.ToListAsync());
        }

        // GET: VinhDanhs/Details/5 - Ai cũng xem được
        [HttpGet("Details/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var vinhDanh = await _context.VinhDanhs
                .FirstOrDefaultAsync(m => m.Id == id);

            if (vinhDanh == null)
                return NotFound();

            return View(vinhDanh);
        }

        // GET: VinhDanhs/Create - Chỉ Admin
        [HttpGet("Create")]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: VinhDanhs/Create - Chỉ Admin
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,HoTen,Loai,SoTienUngHo,SoGioHoatDong,NgayVinhDanh,GhiChu")] VinhDanh vinhDanh)
        {
            if (ModelState.IsValid)
            {
                _context.Add(vinhDanh);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(vinhDanh);
        }

        // GET: VinhDanhs/Edit/5 - Chỉ Admin
        [HttpGet("Edit/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var vinhDanh = await _context.VinhDanhs.FindAsync(id);
            if (vinhDanh == null)
                return NotFound();

            return View(vinhDanh);
        }

        // POST: VinhDanhs/Edit/5 - Chỉ Admin
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,HoTen,Loai,SoTienUngHo,SoGioHoatDong,NgayVinhDanh,GhiChu")] VinhDanh vinhDanh)
        {
            if (id != vinhDanh.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(vinhDanh);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VinhDanhExists(vinhDanh.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(vinhDanh);
        }

        // GET: VinhDanhs/Delete/5 - Chỉ Admin
        [HttpGet("Delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var vinhDanh = await _context.VinhDanhs
                .FirstOrDefaultAsync(m => m.Id == id);

            if (vinhDanh == null)
                return NotFound();

            return View(vinhDanh);
        }

        // POST: VinhDanhs/Delete/5 - Chỉ Admin
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vinhDanh = await _context.VinhDanhs.FindAsync(id);
            if (vinhDanh != null)
            {
                _context.VinhDanhs.Remove(vinhDanh);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool VinhDanhExists(int id)
        {
            return _context.VinhDanhs.Any(e => e.Id == id);
        }

        // GET: VinhDanhs/TaoChungNhan/5 - Chỉ Admin
        [HttpGet("TaoChungNhan/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TaoChungNhan(int id)
        {
            try
            {
                var vinhDanh = await _context.VinhDanhs.FindAsync(id);
                if (vinhDanh == null)
                    return NotFound();

                // Kiểm tra điều kiện: phải là nhà hảo tâm và có số tiền ủng hộ
                var isNhaHaoTam = (vinhDanh.Loai != null && 
                    (vinhDanh.Loai.ToLower().Contains("hảo tâm") || vinhDanh.Loai == "NHT"));
                
                if (!isNhaHaoTam || !vinhDanh.SoTienUngHo.HasValue || vinhDanh.SoTienUngHo.Value <= 0)
                {
                    return BadRequest("Chỉ có thể tạo chứng nhận cho nhà hảo tâm có số tiền ủng hộ.");
                }

                // Tạo chứng nhận trong database
                var fileName = $"ChungNhan_{vinhDanh.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var cn = new ChungNhanQuyenGop
                {
                    VinhDanhId = vinhDanh.Id,
                    SoChungNhan = $"CN-{DateTime.Now.Year}-{vinhDanh.Id:D4}",
                    NoiDung = $"Đã ủng hộ {vinhDanh.SoTienUngHo.Value.ToString("N0")} VNĐ cho Mái ấm Tình Thương",
                    FilePath = fileName,
                    NgayCap = DateTime.UtcNow
                };

                _context.ChungNhanQuyenGops.Add(cn);
                await _context.SaveChangesAsync();

                // Font hỗ trợ tiếng Việt - thử nhiều đường dẫn
                BaseFont baseFont = null;
                string[] fontPaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Arial.ttf"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "ARIAL.TTF"),
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", // Linux/Railway
                    "/System/Library/Fonts/Supplemental/Arial.ttf" // macOS
                };

                foreach (var fontPath in fontPaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(fontPath))
                        {
                            baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                            break;
                        }
                    }
                    catch { }
                }

                // Nếu không tìm thấy font, dùng font mặc định (có thể không hỗ trợ tiếng Việt)
                if (baseFont == null)
                {
                    baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                }

                // Khai báo font
                var fontTitle = new iTextSharp.text.Font(baseFont, 20, iTextSharp.text.Font.BOLD);
                fontTitle.Color = new BaseColor(255, 0, 0); // đỏ sáng

                var fontHeader = new iTextSharp.text.Font(baseFont, 14, iTextSharp.text.Font.BOLD);
                fontHeader.Color = new BaseColor(139, 0, 0); // dark red

                var fontNormal = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.NORMAL);
                fontNormal.Color = BaseColor.Black;

                using (var ms = new MemoryStream())
                {
                    Document doc = new Document(PageSize.A4, 50, 50, 50, 50);
                    PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    // Logo Mái Ấm (nếu có)
                    try
                    {
                        string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo.png");
                        if (System.IO.File.Exists(logoPath))
                        {
                            var logo = Image.GetInstance(logoPath);
                            logo.Alignment = Element.ALIGN_CENTER;
                            logo.ScaleToFit(80f, 80f);
                            doc.Add(logo);
                            doc.Add(new Paragraph("\n"));
                        }
                    }
                    catch
                    {
                        // Bỏ qua nếu logo không tồn tại
                    }

                    // Tiêu đề
                    Paragraph title = new Paragraph("CHỨNG NHẬN QUYÊN GÓP", fontTitle);
                    title.Alignment = Element.ALIGN_CENTER;
                    doc.Add(title);

                    doc.Add(new Paragraph("\n"));

                    // Table thông tin
                    PdfPTable table = new PdfPTable(2);
                    table.WidthPercentage = 80;
                    table.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SetWidths(new float[] { 1f, 2f });

                    void AddRow(string key, string value)
                    {
                        var cellKey = new PdfPCell(new Phrase(key, fontHeader))
                        {
                            Border = Rectangle.NO_BORDER,
                            BackgroundColor = new BaseColor(255, 235, 235), // màu hồng nhạt
                            Padding = 5
                        };
                        var cellValue = new PdfPCell(new Phrase(value, fontNormal))
                        {
                            Border = Rectangle.NO_BORDER,
                            Padding = 5
                        };
                        table.AddCell(cellKey);
                        table.AddCell(cellValue);
                    }

                    AddRow("Số CN", cn.SoChungNhan);
                    AddRow("Họ tên", vinhDanh.HoTen ?? "N/A");
                    AddRow("Số tiền đã ủng hộ", $"{vinhDanh.SoTienUngHo.Value.ToString("N0")} VNĐ");
                    AddRow("Ngày", DateTime.Now.ToString("dd/MM/yyyy"));

                    doc.Add(table);
                    doc.Add(new Paragraph("\n"));

                    // Lời cảm ơn căn giữa
                    Paragraph thanks = new Paragraph("Xin chân thành cảm ơn sự đóng góp của Quý vị!", fontNormal);
                    thanks.Alignment = Element.ALIGN_CENTER;
                    doc.Add(thanks);

                    doc.Add(new Paragraph("\n"));

                    // Line separator trang trí
                    var line = new iTextSharp.text.pdf.draw.LineSeparator(1f, 100f, new BaseColor(255, 0, 0), Element.ALIGN_CENTER, -2);
                    doc.Add(new Chunk(line));

                    doc.Close();

                    return File(ms.ToArray(), "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                // Log lỗi và trả về thông báo
                return StatusCode(500, $"Lỗi khi tạo chứng nhận: {ex.Message}");
            }
        }


    }
}
