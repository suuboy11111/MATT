using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                .Include(m => m.SupporterMaiAms.Where(sma => sma.Supporter.IsApproved))
                    .ThenInclude(sma => sma.Supporter)
                        .ThenInclude(s => s.SupporterSupportTypes)
                            .ThenInclude(sst => sst.SupportType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (maiAm == null) return NotFound();

            ViewBag.TotalSupportRequests = maiAm.SupportRequests.Count;
            ViewBag.ApprovedSupportRequests = maiAm.SupportRequests.Count(r => r.IsApproved);
            ViewBag.TotalSupporters = maiAm.SupporterMaiAms.Count(sma => sma.Supporter.IsApproved);

            return View(maiAm);
        }

    }
}
