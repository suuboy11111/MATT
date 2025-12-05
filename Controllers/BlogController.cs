using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace MaiAmTinhThuong.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly NotificationService _notificationService;

        public BlogController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        // GET: Blog
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["UserId"] = user.Id;

            var blogPosts = await _context.BlogPosts
                                          .Where(b => b.IsApproved)
                                          .Include(b => b.Author)
                                          .Include(b => b.Comments)
                                          .ThenInclude(c => c.Author)
                                          .OrderByDescending(b => b.CreatedAt)
                                          .ToListAsync();

            // Tính số lượt like cho mỗi bài viết và kiểm tra xem người dùng đã like bài viết này chưa
            foreach (var post in blogPosts)
            {
                post.LikeCount = await _context.Likes.CountAsync(l => l.BlogPostId == post.Id);
                post.LikedByUser = await _context.Likes.AnyAsync(l => l.BlogPostId == post.Id && l.UserId == user.Id);
            }

            return View(blogPosts);
        }

        // API: Like/Unlike (AJAX)
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleLike(int blogPostId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var existingLike = await _context.Likes
                                             .FirstOrDefaultAsync(l => l.BlogPostId == blogPostId && l.UserId == user.Id);

            bool isLiked;
            if (existingLike == null)
            {
                var like = new Like
                {
                    BlogPostId = blogPostId,
                    UserId = user.Id
                };
                _context.Likes.Add(like);
                isLiked = true;
            }
            else
            {
                _context.Likes.Remove(existingLike);
                isLiked = false;
            }

            await _context.SaveChangesAsync();

            var likeCount = await _context.Likes.CountAsync(l => l.BlogPostId == blogPostId);

            return Json(new { success = true, isLiked = isLiked, likeCount = likeCount });
        }

        // API: Add Comment (AJAX)
        [HttpPost]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddComment(int blogPostId, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Nội dung bình luận không được để trống" });
            }

            var comment = new Comment
            {
                BlogPostId = blogPostId,
                Content = content.Trim(),
                CreatedAt = DateTime.Now,
                AuthorId = user.Id
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Load lại comment với Author
            await _context.Entry(comment).Reference(c => c.Author).LoadAsync();

            return Json(new
            {
                success = true,
                comment = new
                {
                    id = comment.Id,
                    content = comment.Content,
                    createdAt = comment.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    authorName = comment.Author?.FullName ?? comment.Author?.Email ?? "Unknown",
                    authorAvatar = comment.Author?.ProfilePicture ?? "/images/default1-avatar.png"
                }
            });
        }

        // POST: Blog/Like (Legacy - giữ lại để tương thích)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Like(int blogPostId)
        {
            var result = await ToggleLike(blogPostId);
            return RedirectToAction(nameof(Index));
        }

        // POST: Blog/CreatePost
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost(BlogPost model, IFormFile image)
        {
            ModelState.Remove("image");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("AuthorId");
            ModelState.Remove("Author");
            ModelState.Remove("LikeCount");
            ModelState.Remove("LikedByUser");
            ModelState.Remove("Comments");
            ModelState.Remove("IsApproved");

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var blogPost = new BlogPost
                {
                    Title = model.Title,
                    Content = model.Content,
                    CreatedAt = DateTime.Now,
                    AuthorId = user.Id,
                    IsApproved = false
                };

                if (image != null && image.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("image", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)");
                    }
                    else if (image.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("image", "Kích thước file không được vượt quá 5MB");
                    }
                    else
                    {
                        try
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                            if (!Directory.Exists(uploadDir))
                            {
                                Directory.CreateDirectory(uploadDir);
                            }
                            
                            var imagePath = Path.Combine(uploadDir, uniqueFileName);
                            using (var stream = new FileStream(imagePath, FileMode.Create))
                            {
                                await image.CopyToAsync(stream);
                            }
                            blogPost.ImageUrl = "/images/" + uniqueFileName;
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("image", $"Lỗi khi lưu file: {ex.Message}");
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    _context.BlogPosts.Add(blogPost);
                    await _context.SaveChangesAsync();

                    await _notificationService.NotifyBlogPostCreatedAsync(user.Id, blogPost.Title);
                    await _notificationService.NotifyAdminNewBlogPostAsync(blogPost.Title, blogPost.Id, user.FullName ?? user.Email);

                    TempData["PostPending"] = "Bài viết của bạn đã được gửi và đang chờ duyệt. Cảm ơn bạn đã chia sẻ! 💖";

                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        // POST: Blog/Comment (Legacy - giữ lại để tương thích)
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Comment(int blogPostId, string content)
        {
            var result = await AddComment(blogPostId, content);
            return RedirectToAction("Index");
        }

        // GET: Blog/LoadMorePosts
        public async Task<IActionResult> LoadMorePosts(int page)
        {
            var user = await _userManager.GetUserAsync(User);
            var blogPosts = await _context.BlogPosts
                .Where(b => b.IsApproved)
                .OrderByDescending(b => b.CreatedAt)
                .Skip(page * 5)
                .Take(5)
                .Include(b => b.Author)
                .Include(b => b.Comments)
                .ThenInclude(c => c.Author)
                .ToListAsync();

            if (user != null)
            {
                foreach (var post in blogPosts)
                {
                    post.LikeCount = await _context.Likes.CountAsync(l => l.BlogPostId == post.Id);
                    post.LikedByUser = await _context.Likes.AnyAsync(l => l.BlogPostId == post.Id && l.UserId == user.Id);
                }
            }

            return PartialView("_PostList", blogPosts);
        }

        // GET: Blog/Edit/{id}
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null || blogPost.AuthorId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            return View(blogPost);
        }

        // POST: Blog/Edit/{id}
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BlogPost model, IFormFile image)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var blogPost = await _context.BlogPosts.FindAsync(id);
                    if (blogPost == null || blogPost.AuthorId != _userManager.GetUserId(User))
                    {
                        return NotFound();
                    }

                    blogPost.Title = model.Title;
                    blogPost.Content = model.Content;

                    if (image != null)
                    {
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image.FileName);
                        using (var stream = new FileStream(imagePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }
                        blogPost.ImageUrl = "/images/" + image.FileName;
                    }

                    _context.Update(blogPost);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.BlogPosts.Any(b => b.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Blog/Delete/{id}
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null || blogPost.AuthorId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            return View(blogPost);
        }

        // POST: Blog/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null || blogPost.AuthorId != _userManager.GetUserId(User))
            {
                return NotFound();
            }

            _context.BlogPosts.Remove(blogPost);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
