using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MaiAmTinhThuong.Data;
using MaiAmTinhThuong.Models;
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

        public BlogController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Blog
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);  // Lấy thông tin người dùng hiện tại

            if (user == null)  // Nếu người dùng chưa đăng nhập
            {
                // Bạn có thể hiển thị một thông báo hoặc chuyển hướng đến trang đăng nhập
                return RedirectToAction("Login", "Account"); // Hoặc redirect đến trang đăng nhập
            }

            ViewData["UserId"] = user.Id; // Lưu UserId vào ViewData

            var blogPosts = await _context.BlogPosts
                                          .Include(b => b.Author)  // Bao gồm tác giả
                                          .Include(b => b.Comments) // Bao gồm bình luận của bài viết
                                          .ThenInclude(c => c.Author)  // Bao gồm tác giả của bình luận
                                          .OrderByDescending(b => b.CreatedAt)  // Sắp xếp bài viết theo ngày tạo
                                          .ToListAsync();

            // Tính số lượt like cho mỗi bài viết và kiểm tra xem người dùng đã like bài viết này chưa
            foreach (var post in blogPosts)
            {
                post.LikeCount = await _context.Likes.CountAsync(l => l.BlogPostId == post.Id);
                post.LikedByUser = await _context.Likes.AnyAsync(l => l.BlogPostId == post.Id && l.UserId == user.Id);
            }

            return View(blogPosts);
        }

        // POST: Blog/Like
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Like(int blogPostId)
        {
            var user = await _userManager.GetUserAsync(User);
            var existingLike = await _context.Likes
                                             .FirstOrDefaultAsync(l => l.BlogPostId == blogPostId && l.UserId == user.Id);

            if (existingLike == null)
            {
                var like = new Like
                {
                    BlogPostId = blogPostId,
                    UserId = user.Id
                };
                _context.Likes.Add(like);
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.Likes.Remove(existingLike);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Blog/CreatePost
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreatePost(BlogPost model, IFormFile image)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);

                var blogPost = new BlogPost
                {
                    Title = model.Title,
                    Content = model.Content,
                    CreatedAt = DateTime.Now,
                    AuthorId = user.Id,
                };

                // Nếu có hình ảnh, lưu trữ nó
                if (image != null)
                {
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image.FileName);
                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    blogPost.ImageUrl = "/images/" + image.FileName;  // Lưu đường dẫn hình ảnh
                }

                // Thêm bài viết vào cơ sở dữ liệu
                _context.BlogPosts.Add(blogPost);
                await _context.SaveChangesAsync();

                // Lưu thông báo vào TempData
                TempData["PostPending"] = "Bài viết của bạn đã được gửi và đang chờ duyệt. Cảm ơn bạn đã chia sẻ! 💖";

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // POST: Blog/Comment
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Comment(int blogPostId, string content)
        {
            var user = await _userManager.GetUserAsync(User);

            var comment = new Comment
            {
                BlogPostId = blogPostId,
                Content = content,
                CreatedAt = DateTime.Now,
                AuthorId = user.Id  // Lưu ID của người dùng đã đăng bình luận
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");  // Quay lại trang Index với các bài viết đã có bình luận
        }

        // GET: Blog/LoadMorePosts
        public async Task<IActionResult> LoadMorePosts(int page)
        {
            var blogPosts = await _context.BlogPosts
                .OrderByDescending(b => b.CreatedAt)
                .Skip(page * 5)
                .Take(5)
                .Include(b => b.Author)
                .Include(b => b.Comments) // Bao gồm bình luận khi tải thêm
                .ToListAsync();

            return PartialView("_PostList", blogPosts);
        }
        // GET: Blog/Edit/{id}
        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null || blogPost.AuthorId != _userManager.GetUserId(User))
            {
                // Nếu bài viết không tồn tại hoặc không phải của người dùng hiện tại
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

                    // Cập nhật các thông tin bài viết
                    blogPost.Title = model.Title;
                    blogPost.Content = model.Content;

                    // Nếu có hình ảnh, lưu trữ nó
                    if (image != null)
                    {
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image.FileName);
                        using (var stream = new FileStream(imagePath, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }
                        blogPost.ImageUrl = "/images/" + image.FileName;
                    }

                    // Cập nhật bài viết vào cơ sở dữ liệu
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
                // Nếu bài viết không tồn tại hoặc không phải của người dùng hiện tại
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
