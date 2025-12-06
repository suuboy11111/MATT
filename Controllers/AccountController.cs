using MaiAmTinhThuong.Models;
using MaiAmTinhThuong.Models.ViewModels;
using MaiAmTinhThuong.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Encodings.Web;

namespace MaiAmTinhThuong.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 RoleManager<IdentityRole> roleManager,
                                 EmailService emailService,
                                 ILogger<AccountController> logger,
                                 IConfiguration configuration,
                                 IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            // Kiểm tra Google OAuth có được cấu hình không
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];
            ViewBag.GoogleOAuthEnabled = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);
            
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Role = model.Role,
                ProfilePicture = "/images/default1-avatar.png",
                Gender = model.Gender?.ToString(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                EmailConfirmed = false // Chưa xác nhận email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                // Tạo role nếu chưa có
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                // Gán role cho user
                await _userManager.AddToRoleAsync(user, model.Role);

                // Gửi email xác nhận
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action("ConfirmEmail", "Account", 
                    new { userId = user.Id, token = token }, 
                    Request.Scheme);

                if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(confirmationLink))
                {
                    await _emailService.SendVerificationEmailAsync(user.Email, confirmationLink);
                }

                TempData["Message"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            // Kiểm tra Google OAuth có được cấu hình không
            var googleClientId = _configuration["Authentication:Google:ClientId"];
            var googleClientSecret = _configuration["Authentication:Google:ClientSecret"];
            ViewBag.GoogleOAuthEnabled = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);
            
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            // Kiểm tra email đã được xác nhận chưa (chỉ cần kiểm tra, không yêu cầu xác nhận lại)
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("", "Email chưa được xác nhận. Vui lòng kiểm tra email đã nhận khi đăng ký và click vào link xác nhận.");
                // Hiển thị nút gửi lại email xác nhận
                ViewBag.ShowResendEmail = true;
                ViewBag.UserEmail = model.Email;
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
            
            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            
            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng thử lại sau.");
            }
            else
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Tạo model và gán giá trị cho nó
            var model = new ProfileViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                ProfilePicture = user.ProfilePicture ?? "/images/default1-avatar.png",
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber ?? user.PhoneNumber2
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model, IFormFile profilePicture)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Remove profilePicture khỏi ModelState để không ảnh hưởng đến validation
            ModelState.Remove("profilePicture");

            // Validate mật khẩu nếu có thay đổi
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Vui lòng nhập mật khẩu hiện tại");
                }
                if (model.NewPassword != model.ConfirmPassword)
                {
                    ModelState.AddModelError("ConfirmPassword", "Xác nhận mật khẩu không khớp");
                }
            }

            // Xử lý upload ảnh riêng biệt (không phụ thuộc vào ModelState.IsValid)
            if (profilePicture != null && profilePicture.Length > 0)
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("profilePicture", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)");
                }
                // Validate file size (max 5MB)
                else if (profilePicture.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("profilePicture", "Kích thước file không được vượt quá 5MB");
                }
                else
                {
                    // File hợp lệ, lưu file
                    try
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profiles");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        var filePath = Path.Combine(uploadDir, uniqueFileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await profilePicture.CopyToAsync(stream);
                        }
                        user.ProfilePicture = "/images/profiles/" + uniqueFileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("profilePicture", $"Lỗi khi lưu file: {ex.Message}");
                    }
                }
            }

            // Kiểm tra ModelState sau khi đã xử lý file
            if (ModelState.IsValid)
            {
                // Cập nhật thông tin người dùng
                user.FullName = model.FullName;
                user.Gender = model.Gender;
                user.DateOfBirth = model.DateOfBirth;
                user.Address = model.Address;
                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    user.PhoneNumber2 = model.PhoneNumber;
                }
                user.UpdatedAt = DateTime.Now;

                // Thay đổi mật khẩu nếu có
                if (!string.IsNullOrEmpty(model.CurrentPassword) && !string.IsNullOrEmpty(model.NewPassword))
                {
                    var passwordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(model);
                    }
                }

                // Cập nhật thông tin người dùng trong DB
                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    TempData["Message"] = "Thông tin tài khoản đã được cập nhật thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }

            // Nếu có lỗi, load lại model với dữ liệu user
            model.Email = user.Email ?? "";
            model.ProfilePicture = user.ProfilePicture ?? "/images/default1-avatar.png";
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteProfilePicture()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Xóa ảnh đại diện
            user.ProfilePicture = "/images/default1-avatar.png"; // Đặt lại ảnh mặc định
            var updateResult = await _userManager.UpdateAsync(user);
            if (updateResult.Succeeded)
            {
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false });
            }
        }

        // Email Confirmation
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                TempData["Message"] = "Email đã được xác nhận thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Login");
            }

            TempData["Error"] = "Link xác nhận không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction("Login");
        }

        // Resend confirmation email
        [HttpPost]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] Dictionary<string, string>? request)
        {
            var email = request?.GetValueOrDefault("email");
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Email không được để trống." });
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Json(new { success = false, message = "Email không tồn tại trong hệ thống." });
            }

            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return Json(new { success = false, message = "Email đã được xác nhận rồi." });
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account",
                new { userId = user.Id, token = token },
                Request.Scheme);

            bool emailSent = false;
            if (!string.IsNullOrEmpty(user.Email) && !string.IsNullOrEmpty(confirmationLink))
            {
                emailSent = await _emailService.SendVerificationEmailAsync(user.Email, confirmationLink);
            }
            
            if (emailSent)
            {
                return Json(new { success = true, message = "Email xác nhận đã được gửi lại. Vui lòng kiểm tra hộp thư." });
            }
            else
            {
                return Json(new { success = false, message = "Không thể gửi email. Vui lòng thử lại sau." });
            }
        }

        // Google OAuth
        [HttpGet]
        public IActionResult GoogleLogin()
        {
            try
            {
                // Đảm bảo sử dụng https trong production (Railway sử dụng reverse proxy)
                var scheme = Request.Scheme;
                // Kiểm tra X-Forwarded-Proto header (Railway set header này)
                if (Request.Headers.ContainsKey("X-Forwarded-Proto"))
                {
                    scheme = Request.Headers["X-Forwarded-Proto"].ToString();
                }
                // Hoặc nếu không phải development, luôn dùng https
                else if (!_environment.IsDevelopment())
                {
                    scheme = "https";
                }
                
                var redirectUrl = Url.Action("GoogleCallback", "Account", null, scheme);
                _logger.LogInformation($"Google OAuth redirect URI: {redirectUrl}");
                
                var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
                return Challenge(properties, "Google");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating Google login");
                TempData["Error"] = "Google OAuth chưa được cấu hình. Vui lòng đăng nhập bằng email và mật khẩu.";
                return RedirectToAction("Login");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            try
            {
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    _logger.LogWarning("Failed to get external login info from Google");
                    TempData["Error"] = "Không thể lấy thông tin từ Google. Vui lòng thử lại hoặc đăng nhập bằng email và mật khẩu.";
                    return RedirectToAction("Login");
                }

                var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
                var name = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "";
                var picture = info.Principal.FindFirst("picture")?.Value ?? "";

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email not found in Google claims");
                    TempData["Error"] = "Không thể lấy email từ Google.";
                    return RedirectToAction("Login");
                }

                // Tìm user đã tồn tại
                var user = await _userManager.FindByEmailAsync(email);
                
                if (user == null)
                {
                    // Tạo user mới
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = name ?? "Người dùng Google",
                        ProfilePicture = picture ?? "/images/default1-avatar.png",
                        EmailConfirmed = true, // Google đã xác nhận email
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        Role = "NguoiHoTro" // Mặc định
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to create user from Google: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                        TempData["Error"] = "Không thể tạo tài khoản. " + string.Join(", ", result.Errors.Select(e => e.Description));
                        return RedirectToAction("Login");
                    }
                    
                    // Tạo role nếu chưa có
                    if (!await _roleManager.RoleExistsAsync("NguoiHoTro"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("NguoiHoTro"));
                    }
                    
                    // Gán role cho user
                    await _userManager.AddToRoleAsync(user, "NguoiHoTro");
                }

                // Thêm external login nếu chưa có
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded && !addLoginResult.Errors.Any(e => e.Code == "LoginAlreadyAssociated"))
                {
                    _logger.LogError("Failed to add external login: {Errors}", string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
                    TempData["Error"] = "Không thể liên kết tài khoản Google.";
                    return RedirectToAction("Login");
                }

                // Đăng nhập
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation($"User {user.Email} logged in via Google");
                return RedirectToLocal(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GoogleCallback");
                TempData["Error"] = "Có lỗi xảy ra khi đăng nhập bằng Google. Vui lòng thử lại hoặc đăng nhập bằng email và mật khẩu.";
                return RedirectToAction("Login");
            }
        }
    }
}
