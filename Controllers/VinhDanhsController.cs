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

namespace MaiAmTinhThuong.Controllers
{
  
    public class VinhDanhsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VinhDanhsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: VinhDanhs
        //[HttpGet("")]
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

        // GET: VinhDanhs/Details/5
        [HttpGet("Details/{id}")]
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

        // GET: VinhDanhs/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: VinhDanhs/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
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

        // GET: VinhDanhs/Edit/5
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var vinhDanh = await _context.VinhDanhs.FindAsync(id);
            if (vinhDanh == null)
                return NotFound();

            return View(vinhDanh);
        }

        // POST: VinhDanhs/Edit/5
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
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

        // GET: VinhDanhs/Delete/5
        [HttpGet("Delete/{id}")]
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

        // POST: VinhDanhs/Delete/5
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
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

        // GET: VinhDanhs/TaoChungNhan/5
        [HttpGet("TaoChungNhan/{id}")]
        public async Task<IActionResult> TaoChungNhan(int id)
        {
            var vinhDanh = await _context.VinhDanhs.FindAsync(id);
            if (vinhDanh == null || !vinhDanh.Loai.ToLower().Contains("hảo tâm"))
                return NotFound();

            // Tạo chứng nhận trong database
            var cn = new ChungNhanQuyenGop
            {
                VinhDanhId = vinhDanh.Id,
                SoChungNhan = $"CN-{DateTime.Now.Year}-{vinhDanh.Id:D4}",
                NoiDung = $"Đã ủng hộ {vinhDanh.SoTienUngHo?.ToString("N0")} VNĐ cho Mái ấm Tình Thương"
            };

            _context.ChungNhanQuyenGops.Add(cn);
            await _context.SaveChangesAsync();

            // Font hỗ trợ tiếng Việt
            string fontPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
            var baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);

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
                string logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo.png");
                if (System.IO.File.Exists(logoPath))
                {
                    var logo = Image.GetInstance(logoPath);
                    logo.Alignment = Element.ALIGN_CENTER;
                    logo.ScaleToFit(80f, 80f);
                    doc.Add(logo);
                    doc.Add(new Paragraph("\n"));
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
                AddRow("Họ tên", vinhDanh.HoTen);
                AddRow("Số tiền đã ủng hộ", $"{vinhDanh.SoTienUngHo:N0} VNĐ");
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

                return File(ms.ToArray(), "application/pdf", $"ChungNhan_{vinhDanh.Id}.pdf");
            }
        }


    }
}
