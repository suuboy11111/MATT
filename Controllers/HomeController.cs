using System.Diagnostics;
using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using Microsoft.AspNetCore.Mvc;

namespace MaiAmTinhThuong.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult About()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Contact(ContactResponse model)
        {
            if (ModelState.IsValid)
            {
                var contactResponse = new ContactResponse
                {
                    Name = model.Name,
                    Email = model.Email,
                    Message = model.Message,
                };

                _context.ContactResponses.Add(contactResponse);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Cảm ơn bạn đã gửi phản hồi!";
                return RedirectToAction("Index", "Home");
            }

            TempData["Message"] = "Vui lòng điền đầy đủ thông tin.";
            return RedirectToAction("Index", "Home");
        }
    }
}
