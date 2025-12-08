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

                // Font hỗ trợ tiếng Việt - ưu tiên font có sẵn trong project, sau đó tìm trên hệ thống
                BaseFont baseFont = null;
                string[] fontPaths = {
                    // Font trong project (nếu có) - ưu tiên cao nhất
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "arial.ttf"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "Arial.ttf"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "times.ttf"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "TimesNewRoman.ttf"),
                    // Windows fonts - Times New Roman hỗ trợ tiếng Việt tốt
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "times.ttf"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "timesi.ttf"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "timesbd.ttf"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "Arial.ttf"),
                    // Linux/Railway fonts - DejaVu và Liberation hỗ trợ Unicode tốt
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf",
                    "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
                    "/usr/share/fonts/truetype/liberation/LiberationSerif-Regular.ttf",
                    "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
                    "/usr/share/fonts/truetype/noto/NotoSerif-Regular.ttf",
                    // macOS fonts
                    "/System/Library/Fonts/Supplemental/Times New Roman.ttf",
                    "/System/Library/Fonts/Supplemental/Arial.ttf"
                };

                foreach (var fontPath in fontPaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(fontPath))
                        {
                            // Dùng IDENTITY_H để hỗ trợ Unicode tiếng Việt đầy đủ
                            // EMBEDDED để đảm bảo font được embed vào PDF
                            baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng tiếp tục thử font khác
                        System.Diagnostics.Debug.WriteLine($"Không thể load font {fontPath}: {ex.Message}");
                    }
                }

                // Nếu không tìm thấy font, thử tạo font từ resource hoặc dùng fallback
                if (baseFont == null)
                {
                    // Thử tìm DejaVu Sans trên Linux (thường có sẵn)
                    try
                    {
                        baseFont = BaseFont.CreateFont("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    }
                    catch
                    {
                        // Fallback: Dùng font mặc định nhưng vẫn cố gắng dùng IDENTITY_H
                        // Lưu ý: Font này có thể không hiển thị tiếng Việt đúng
                        try
                        {
                            baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                        }
                        catch
                        {
                            baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        }
                    }
                }

                // Khai báo các font với kích thước và màu sắc chuyên nghiệp
                var fontTitle = new iTextSharp.text.Font(baseFont, 28, iTextSharp.text.Font.BOLD);
                fontTitle.Color = new BaseColor(184, 0, 0); // Đỏ đậm chuyên nghiệp

                var fontSubtitle = new iTextSharp.text.Font(baseFont, 14, iTextSharp.text.Font.NORMAL);
                fontSubtitle.Color = new BaseColor(100, 100, 100); // Xám đậm

                var fontHeader = new iTextSharp.text.Font(baseFont, 11, iTextSharp.text.Font.BOLD);
                fontHeader.Color = new BaseColor(60, 60, 60); // Xám đen

                var fontValue = new iTextSharp.text.Font(baseFont, 11, iTextSharp.text.Font.NORMAL);
                fontValue.Color = BaseColor.Black;

                var fontValueBold = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.BOLD);
                fontValueBold.Color = new BaseColor(0, 100, 0); // Xanh lá đậm cho số tiền

                var fontThanks = new iTextSharp.text.Font(baseFont, 13, iTextSharp.text.Font.ITALIC);
                fontThanks.Color = new BaseColor(80, 80, 80);

                var fontFooter = new iTextSharp.text.Font(baseFont, 10, iTextSharp.text.Font.NORMAL);
                fontFooter.Color = new BaseColor(120, 120, 120);

                using (var ms = new MemoryStream())
                {
                    Document doc = new Document(PageSize.A4, 60, 60, 80, 60);
                    PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    // Tạo border trang trí cho toàn bộ trang
                    var borderColor = new BaseColor(200, 200, 200);
                    var borderWidth = 2f;
                    var pageSize = doc.PageSize;
                    
                    // Vẽ border đẹp
                    var canvas = writer.DirectContent;
                    canvas.SetColorStroke(borderColor);
                    canvas.SetLineWidth(borderWidth);
                    canvas.Rectangle(40, 40, pageSize.Width - 80, pageSize.Height - 80);
                    canvas.Stroke();

                    // Vẽ border trong nhỏ hơn
                    canvas.SetLineWidth(1f);
                    canvas.SetColorStroke(new BaseColor(220, 220, 220));
                    canvas.Rectangle(50, 50, pageSize.Width - 100, pageSize.Height - 100);
                    canvas.Stroke();

                    // Logo Mái Ấm (nếu có)
                    try
                    {
                        string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo.png");
                        if (System.IO.File.Exists(logoPath))
                        {
                            var logo = Image.GetInstance(logoPath);
                            logo.Alignment = Element.ALIGN_CENTER;
                            logo.ScaleToFit(100f, 100f);
                            doc.Add(logo);
                            doc.Add(new Paragraph("\n"));
                        }
                    }
                    catch
                    {
                        // Bỏ qua nếu logo không tồn tại
                    }

                    // Thêm khoảng trống
                    doc.Add(new Paragraph("\n"));

                    // Tiêu đề chính - CHỨNG NHẬN QUYÊN GÓP
                    Paragraph title = new Paragraph("CHỨNG NHẬN QUYÊN GÓP", fontTitle);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 5f;
                    doc.Add(title);

                    // Phụ đề
                    Paragraph subtitle = new Paragraph("Mái Ấm Tình Thương", fontSubtitle);
                    subtitle.Alignment = Element.ALIGN_CENTER;
                    subtitle.SpacingAfter = 30f;
                    doc.Add(subtitle);

                    // Vẽ đường kẻ trang trí dưới tiêu đề
                    canvas.SetColorStroke(new BaseColor(184, 0, 0));
                    canvas.SetLineWidth(2f);
                    canvas.MoveTo(150, doc.Top - 180);
                    canvas.LineTo(pageSize.Width - 150, doc.Top - 180);
                    canvas.Stroke();

                    doc.Add(new Paragraph("\n\n"));

                    // Table thông tin với thiết kế đẹp hơn
                    PdfPTable table = new PdfPTable(2);
                    table.WidthPercentage = 75;
                    table.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SetWidths(new float[] { 1.2f, 2.8f });
                    table.SpacingBefore = 20f;
                    table.SpacingAfter = 30f;

                    void AddRow(string key, string value, bool isValueBold = false)
                    {
                        // Cell label
                        var cellKey = new PdfPCell(new Phrase(key, fontHeader))
                        {
                            Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER | Rectangle.TOP_BORDER | Rectangle.BOTTOM_BORDER,
                            BorderColor = new BaseColor(230, 230, 230),
                            BorderWidth = 1f,
                            BackgroundColor = new BaseColor(250, 250, 250),
                            Padding = 12f,
                            PaddingLeft = 15f,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };

                        // Cell value
                        var valueFont = isValueBold ? fontValueBold : fontValue;
                        var cellValue = new PdfPCell(new Phrase(value, valueFont))
                        {
                            Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER | Rectangle.TOP_BORDER | Rectangle.BOTTOM_BORDER,
                            BorderColor = new BaseColor(230, 230, 230),
                            BorderWidth = 1f,
                            Padding = 12f,
                            PaddingLeft = 15f,
                            VerticalAlignment = Element.ALIGN_MIDDLE
                        };

                        table.AddCell(cellKey);
                        table.AddCell(cellValue);
                    }

                    AddRow("Số chứng nhận", cn.SoChungNhan);
                    AddRow("Họ và tên", vinhDanh.HoTen ?? "N/A");
                    AddRow("Số tiền đã ủng hộ", $"{vinhDanh.SoTienUngHo.Value.ToString("N0")} VNĐ", true);
                    AddRow("Ngày cấp", DateTime.Now.ToString("dd/MM/yyyy"));

                    doc.Add(table);

                    // Lời cảm ơn với thiết kế đẹp hơn
                    doc.Add(new Paragraph("\n"));
                    Paragraph thanks = new Paragraph("Xin chân thành cảm ơn sự đóng góp quý báu của Quý vị!", fontThanks);
                    thanks.Alignment = Element.ALIGN_CENTER;
                    thanks.SpacingBefore = 20f;
                    thanks.SpacingAfter = 10f;
                    doc.Add(thanks);

                    // Thêm dấu ngoặc kép trang trí
                    Paragraph quote = new Paragraph("\"Tình yêu thương là ngôn ngữ mà người điếc có thể nghe và người mù có thể thấy.\"", fontFooter);
                    quote.Alignment = Element.ALIGN_CENTER;
                    quote.SpacingAfter = 30f;
                    doc.Add(quote);

                    // Footer với thông tin
                    Paragraph footer = new Paragraph("Mái Ấm Tình Thương - Nơi trao gửi yêu thương", fontFooter);
                    footer.Alignment = Element.ALIGN_CENTER;
                    footer.SpacingBefore = 40f;
                    doc.Add(footer);

                    // Vẽ đường kẻ trang trí trên footer
                    canvas.SetColorStroke(new BaseColor(200, 200, 200));
                    canvas.SetLineWidth(1f);
                    canvas.MoveTo(150, doc.Bottom + 50);
                    canvas.LineTo(pageSize.Width - 150, doc.Bottom + 50);
                    canvas.Stroke();

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
