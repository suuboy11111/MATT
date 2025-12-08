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
using System.Text;

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

                // Đăng ký CodePagesEncodingProvider để hỗ trợ encoding Unicode
                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                }
                catch { }

                // Font hỗ trợ tiếng Việt - ưu tiên Noto Sans trong project (đã tải sẵn)
                BaseFont baseFont = null;
                BaseFont baseFontBold = null;
                
                string[] fontPaths = {
                    // Font trong project - ưu tiên cao nhất (Noto Sans hỗ trợ tiếng Việt hoàn hảo)
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "NotoSans-Regular.ttf"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "NotoSans-Bold.ttf"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "arial.ttf"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "Arial.ttf"),
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "times.ttf"),
                    // Linux/Railway fonts - Noto Sans thường có sẵn
                    "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
                    "/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
                    "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
                    // Windows fonts
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "times.ttf"),
                    // macOS fonts
                    "/System/Library/Fonts/Supplemental/Arial.ttf"
                };

                // Tìm font Regular - dùng Identity-H (Unicode) để hỗ trợ đầy đủ tiếng Việt
                foreach (var fontPath in fontPaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(fontPath) && !fontPath.Contains("Bold"))
                        {
                            // Thử dùng Identity-H (Unicode) - tốt nhất cho tiếng Việt
                            try
                            {
                                // Dùng string "Identity-H" thay vì constant để tránh lỗi
                                baseFont = BaseFont.CreateFont(fontPath, "Identity-H", BaseFont.EMBEDDED);
                            }
                            catch
                            {
                                // Nếu Identity-H không được, thử dùng UTF-8 encoding
                                try
                                {
                                    baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                                }
                                catch
                                {
                                    // Fallback cuối cùng: CP1252 (vẫn hỗ trợ một phần tiếng Việt)
                                    baseFont = BaseFont.CreateFont(fontPath, BaseFont.CP1252, BaseFont.EMBEDDED);
                                }
                            }
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Không thể load font {fontPath}: {ex.Message}");
                    }
                }

                // Tìm font Bold
                string[] boldFontPaths = {
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "fonts", "NotoSans-Bold.ttf"),
                    "/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf",
                    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
                };

                foreach (var fontPath in boldFontPaths)
                {
                    try
                    {
                        if (System.IO.File.Exists(fontPath))
                        {
                            try
                            {
                                baseFontBold = BaseFont.CreateFont(fontPath, "Identity-H", BaseFont.EMBEDDED);
                            }
                            catch
                            {
                                try
                                {
                                    baseFontBold = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                                }
                                catch
                                {
                                    baseFontBold = BaseFont.CreateFont(fontPath, BaseFont.CP1252, BaseFont.EMBEDDED);
                                }
                            }
                            break;
                        }
                    }
                    catch { }
                }

                // Nếu không tìm thấy Bold, dùng Regular làm Bold
                if (baseFontBold == null && baseFont != null)
                {
                    baseFontBold = baseFont;
                }

                // Fallback cuối cùng
                if (baseFont == null)
                {
                    try
                    {
                        try
                        {
                            baseFont = BaseFont.CreateFont("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "Identity-H", BaseFont.EMBEDDED);
                        }
                        catch
                        {
                            baseFont = BaseFont.CreateFont("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", BaseFont.CP1252, BaseFont.EMBEDDED);
                        }
                        baseFontBold = baseFont;
                    }
                    catch
                    {
                        // Cuối cùng dùng font mặc định
                        baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                        baseFontBold = baseFont;
                    }
                }

                // Khai báo các font với kích thước và màu sắc chuyên nghiệp - dùng baseFontBold cho Bold
                var fontTitle = new iTextSharp.text.Font(baseFontBold ?? baseFont, 32, iTextSharp.text.Font.BOLD);
                fontTitle.Color = new BaseColor(200, 30, 30); // Đỏ đậm chuyên nghiệp

                var fontSubtitle = new iTextSharp.text.Font(baseFont, 16, iTextSharp.text.Font.NORMAL);
                fontSubtitle.Color = new BaseColor(120, 120, 120); // Xám đậm

                var fontHeader = new iTextSharp.text.Font(baseFontBold ?? baseFont, 12, iTextSharp.text.Font.BOLD);
                fontHeader.Color = new BaseColor(50, 50, 50); // Xám đen

                var fontValue = new iTextSharp.text.Font(baseFont, 12, iTextSharp.text.Font.NORMAL);
                fontValue.Color = BaseColor.Black;

                var fontValueBold = new iTextSharp.text.Font(baseFontBold ?? baseFont, 13, iTextSharp.text.Font.BOLD);
                fontValueBold.Color = new BaseColor(0, 120, 0); // Xanh lá đậm cho số tiền

                var fontThanks = new iTextSharp.text.Font(baseFont, 14, iTextSharp.text.Font.ITALIC);
                fontThanks.Color = new BaseColor(70, 70, 70);

                var fontQuote = new iTextSharp.text.Font(baseFont, 11, iTextSharp.text.Font.ITALIC);
                fontQuote.Color = new BaseColor(100, 100, 100);

                var fontFooter = new iTextSharp.text.Font(baseFont, 10, iTextSharp.text.Font.NORMAL);
                fontFooter.Color = new BaseColor(130, 130, 130);

                using (var ms = new MemoryStream())
                {
                    Document doc = new Document(PageSize.A4, 60, 60, 80, 60);
                    PdfWriter writer = PdfWriter.GetInstance(doc, ms);
                    doc.Open();

                    // Tạo border trang trí đơn giản và chuyên nghiệp
                    var pageSize = doc.PageSize;
                    var canvas = writer.DirectContent;
                    
                    // Chỉ vẽ một border mỏng, đẹp
                    canvas.SetColorStroke(new BaseColor(200, 200, 200));
                    canvas.SetLineWidth(1.5f);
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

                    // Thêm khoảng trống sau subtitle
                    doc.Add(new Paragraph("\n"));

                    // Table thông tin với thiết kế đẹp hơn
                    PdfPTable table = new PdfPTable(2);
                    table.WidthPercentage = 75;
                    table.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.SetWidths(new float[] { 1.2f, 2.8f });
                    table.SpacingBefore = 20f;
                    table.SpacingAfter = 30f;

                    void AddRow(string key, string value, bool isValueBold = false)
                    {
                        // Cell label - thiết kế đẹp hơn
                        var cellKey = new PdfPCell(new Phrase(key, fontHeader))
                        {
                            Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER | Rectangle.TOP_BORDER | Rectangle.BOTTOM_BORDER,
                            BorderColor = new BaseColor(220, 220, 220),
                            BorderWidth = 0.5f,
                            BackgroundColor = new BaseColor(248, 248, 248),
                            Padding = 14f,
                            PaddingLeft = 18f,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_LEFT
                        };

                        // Cell value - không có underline hay line thừa
                        var valueFont = isValueBold ? fontValueBold : fontValue;
                        var cellValue = new PdfPCell(new Phrase(value, valueFont))
                        {
                            Border = Rectangle.LEFT_BORDER | Rectangle.RIGHT_BORDER | Rectangle.TOP_BORDER | Rectangle.BOTTOM_BORDER,
                            BorderColor = new BaseColor(220, 220, 220),
                            BorderWidth = 0.5f,
                            BackgroundColor = BaseColor.White,
                            Padding = 14f,
                            PaddingLeft = 18f,
                            VerticalAlignment = Element.ALIGN_MIDDLE,
                            HorizontalAlignment = Element.ALIGN_LEFT
                        };

                        table.AddCell(cellKey);
                        table.AddCell(cellValue);
                    }

                    AddRow("Số chứng nhận", cn.SoChungNhan);
                    AddRow("Họ và tên", vinhDanh.HoTen ?? "N/A");
                    AddRow("Số tiền đã ủng hộ", $"{vinhDanh.SoTienUngHo.Value.ToString("N0")} VNĐ", true);
                    AddRow("Ngày cấp", DateTime.Now.ToString("dd/MM/yyyy"));

                    doc.Add(table);

                    // Lời cảm ơn với thiết kế đẹp và chuyên nghiệp
                    doc.Add(new Paragraph("\n\n"));
                    
                    // Vẽ đường kẻ trang trí mỏng trước lời cảm ơn
                    var currentY = writer.GetVerticalPosition(false);
                    canvas.SetColorStroke(new BaseColor(220, 220, 220));
                    canvas.SetLineWidth(1f);
                    canvas.MoveTo(150, currentY - 10);
                    canvas.LineTo(pageSize.Width - 150, currentY - 10);
                    canvas.Stroke();
                    
                    Paragraph thanks = new Paragraph("Xin chân thành cảm ơn sự đóng góp quý báu của Quý vị!", fontThanks);
                    thanks.Alignment = Element.ALIGN_CENTER;
                    thanks.SpacingBefore = 25f;
                    thanks.SpacingAfter = 20f;
                    doc.Add(thanks);

                    // Quote với style đẹp hơn - không có dấu ngoặc kép thừa
                    Paragraph quote = new Paragraph("Tình yêu thương là ngôn ngữ mà người điếc có thể nghe và người mù có thể thấy.", fontQuote);
                    quote.Alignment = Element.ALIGN_CENTER;
                    quote.SpacingAfter = 40f;
                    doc.Add(quote);

                    // Footer với thông tin - đơn giản và chuyên nghiệp
                    Paragraph footer = new Paragraph("Mái Ấm Tình Thương - Nơi trao gửi yêu thương", fontFooter);
                    footer.Alignment = Element.ALIGN_CENTER;
                    footer.SpacingBefore = 30f;
                    doc.Add(footer);

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
