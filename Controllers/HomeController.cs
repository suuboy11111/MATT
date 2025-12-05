using System.Diagnostics;
using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Models.ViewModels;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MaiAmTinhThuong.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, NotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy 3 bài viết có tương tác cao nhất (đã được duyệt)
            // Tương tác = số lượt like + số comment
            // Tối ưu query để tránh N+1 problem bằng cách sử dụng GroupBy
            var blogPostIds = await _context.BlogPosts
                .Where(b => b.IsApproved)
                .Select(b => b.Id)
                .ToListAsync();

            // Tính like và comment count cho tất cả bài viết một lần
            var likeCounts = await _context.Likes
                .Where(l => blogPostIds.Contains(l.BlogPostId))
                .GroupBy(l => l.BlogPostId)
                .Select(g => new { BlogPostId = g.Key, Count = g.Count() })
                .ToListAsync();

            var commentCounts = await _context.Comments
                .Where(c => blogPostIds.Contains(c.BlogPostId))
                .GroupBy(c => c.BlogPostId)
                .Select(g => new { BlogPostId = g.Key, Count = g.Count() })
                .ToListAsync();

            var likeDict = likeCounts.ToDictionary(x => x.BlogPostId, x => x.Count);
            var commentDict = commentCounts.ToDictionary(x => x.BlogPostId, x => x.Count);

            // Lấy top 3 bài viết với interaction count
            var topBlogPosts = await _context.BlogPosts
                .Where(b => b.IsApproved)
                .Include(b => b.Author)
                .Select(b => new BlogPostViewModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    Content = b.Content,
                    ImageUrl = b.ImageUrl,
                    CreatedAt = b.CreatedAt,
                    AuthorName = b.Author != null ? (b.Author.FullName ?? b.Author.Email ?? "Không xác định") : "Không xác định",
                    LikeCount = likeDict.ContainsKey(b.Id) ? likeDict[b.Id] : 0,
                    CommentCount = commentDict.ContainsKey(b.Id) ? commentDict[b.Id] : 0,
                    TotalInteraction = 0 // Sẽ tính sau
                })
                .ToListAsync();

            // Tính TotalInteraction và sắp xếp
            foreach (var post in topBlogPosts)
            {
                post.TotalInteraction = post.LikeCount + post.CommentCount;
            }

            topBlogPosts = topBlogPosts
                .OrderByDescending(x => x.TotalInteraction)
                .ThenByDescending(x => x.CreatedAt)
                .Take(3)
                .ToList();

            ViewBag.TopBlogPosts = topBlogPosts;
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
                    SubmittedAt = DateTime.Now
                };

                _context.ContactResponses.Add(contactResponse);
                await _context.SaveChangesAsync();

                // Gửi thông báo cho tất cả admin
                await _notificationService.NotifyAdminNewContactAsync(model.Name, model.Email, contactResponse.Id);

                TempData["Message"] = "Cảm ơn bạn đã gửi phản hồi! Chúng tôi sẽ liên hệ với bạn sớm nhất có thể.";
                return RedirectToAction("Index", "Home");
            }

            TempData["Message"] = "Vui lòng điền đầy đủ thông tin.";
            return RedirectToAction("Index", "Home");
        }
    }
}
