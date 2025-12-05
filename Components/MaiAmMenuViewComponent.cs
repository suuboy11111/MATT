using MaiAmTinhThuong.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MaiAmTinhThuong.Components
{
    public class MaiAmMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public MaiAmMenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var maiAms = await _context.MaiAms.OrderBy(m => m.Id).ToListAsync();
            return View(maiAms);
        }
    }
}
