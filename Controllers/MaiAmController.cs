using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaiAmTinhThuong.Controllers
{
    
    public class MaiAmController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MaiAmController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MaiAm/Details/5
        // Trang chi tiết mái ấm, có thể hiện tab quản lý người cần hỗ trợ, người hỗ trợ, thống kê
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var maiAm = await _context.MaiAms
                .Include(m => m.SupportRequests.Where(r => r.IsApproved))  // Lọc hồ sơ đã duyệt
                .Include(m => m.SupporterMaiAms.Where(sma => sma.Supporter != null && sma.Supporter.IsApproved))
                    .ThenInclude(sma => sma.Supporter)
                        .ThenInclude(s => s.SupporterSupportTypes)
                            .ThenInclude(sst => sst.SupportType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maiAm == null) return NotFound();

            // Khởi tạo collections nếu null
            if (maiAm.SupportRequests == null)
                maiAm.SupportRequests = new List<SupportRequest>();
            
            if (maiAm.SupporterMaiAms == null)
                maiAm.SupporterMaiAms = new List<SupporterMaiAm>();

            ViewBag.TotalSupportRequests = maiAm.SupportRequests?.Count ?? 0;
            ViewBag.ApprovedSupportRequests = maiAm.SupportRequests?.Count(r => r.IsApproved) ?? 0;
            ViewBag.TotalSupporters = maiAm.SupporterMaiAms?.Count(sma => sma.Supporter != null && sma.Supporter.IsApproved) ?? 0;

            return View(maiAm);
        }

    }
}
