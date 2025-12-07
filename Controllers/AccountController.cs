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
        private readonly VerificationCodeService _verificationCodeService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public AccountController(UserManager<ApplicationUser> userManager,
                                 SignInManager<ApplicationUser> signInManager,
                                 RoleManager<IdentityRole> roleManager,
                                 EmailService emailService,
                                 VerificationCodeService verificationCodeService,
                                 ILogger<AccountController> logger,
                                 IConfiguration configuration,
                                 IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _verificationCodeService = verificationCodeService;
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
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

                // Tạo và gửi mã xác nhận
                try
                {
                    var verificationCode = _verificationCodeService.GenerateCode();
                    _verificationCodeService.StoreCode(user.Email!, verificationCode, user.Id);
                    _logger.LogInformation($"Verification code generated and stored for {user.Email}");

                    // Gửi email với timeout
                    _logger.LogInformation($"Attempting to send verification code email to {user.Email}...");
                    
                    // Sử dụng Task.Run để không block request quá lâu
                    var emailTask = _emailService.SendVerificationCodeAsync(user.Email!, verificationCode);
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(15)); // Timeout 15 giây
                    
                    var completedTask = await Task.WhenAny(emailTask, timeoutTask);
                    
                    bool emailSent = false;
                    if (completedTask == emailTask)
                    {
                        emailSent = await emailTask;
                        _logger.LogInformation($"Email send result: {emailSent}");
                    }
                    else
                    {
                        _logger.LogWarning($"Email sending timeout for {user.Email}");
                        emailSent = false;
                    }

                    if (emailSent)
                    {
                        TempData["Message"] = "Đăng ký thành công! Vui lòng kiểm tra email để lấy mã xác nhận (có thể trong thư mục Spam).";
                        TempData["Email"] = user.Email; // Lưu email để chuyển sang trang verify
                        _logger.LogInformation($"Registration successful for {user.Email}, redirecting to VerifyCode");
                        return RedirectToAction("VerifyCode", new { email = user.Email });
                    }
                    else
                    {
                        // Vẫn cho phép user tiếp tục, họ có thể yêu cầu gửi lại mã
                        TempData["Warning"] = "Đăng ký thành công! Tuy nhiên, không thể gửi email xác nhận ngay bây giờ. Bạn có thể yêu cầu gửi lại mã xác nhận.";
                        _logger.LogWarning($"Failed to send verification code to {user.Email}, but allowing user to continue");
                        return RedirectToAction("VerifyCode", new { email = user.Email });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending verification code during registration");
                    // Vẫn cho phép user tiếp tục, họ có thể yêu cầu gửi lại mã
                    TempData["Warning"] = "Đăng ký thành công! Tuy nhiên, có lỗi khi gửi email xác nhận. Bạn có thể yêu cầu gửi lại mã xác nhận.";
                    return RedirectToAction("VerifyCode", new { email = user.Email });
                }
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

            // Kiểm tra email đã được xác nhận chưa
            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("", "Email chưa được xác nhận. Vui lòng kiểm tra email và nhập mã xác nhận 6 số đã được gửi đến email của bạn.");
                // Hiển thị nút chuyển đến trang verify code
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
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(profilePicture.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("profilePicture", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)");
                }
                else if (profilePicture.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("profilePicture", "Kích thước file không được vượt quá 5MB");
                }
                else
                {
                    try
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                        var uploadDir = Path.Combine(_environment.WebRootPath, "images", "profiles");
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
                        _logger.LogInformation($"Profile picture uploaded: {user.ProfilePicture}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error uploading profile picture");
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
                // Đảm bảo DateOfBirth là UTC nếu có (PostgreSQL yêu cầu)
                if (model.DateOfBirth.HasValue)
                {
                    // DateOfBirth thường là local date, chỉ lấy date part và convert sang UTC
                    var dateOnly = model.DateOfBirth.Value.Date;
                    user.DateOfBirth = new DateTime(dateOnly.Ticks, DateTimeKind.Utc);
                }
                else
                {
                    user.DateOfBirth = null;
                }
                user.Address = model.Address;
                if (!string.IsNullOrEmpty(model.PhoneNumber))
                {
                    user.PhoneNumber2 = model.PhoneNumber;
                }
                user.UpdatedAt = DateTime.UtcNow;
                
                // Đảm bảo CreatedAt là UTC nếu có
                if (user.CreatedAt.HasValue && user.CreatedAt.Value.Kind != DateTimeKind.Utc)
                {
                    user.CreatedAt = user.CreatedAt.Value.ToUniversalTime();
                }

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
                try
                {
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (updateResult.Succeeded)
                    {
                        TempData["Message"] = "Thông tin tài khoản đã được cập nhật thành công!";
                        return RedirectToAction("Profile");
                    }
                    else
                    {
                        var errorMessages = updateResult.Errors.Select(e => e.Description).ToList();
                        TempData["Error"] = string.Join(" ", errorMessages);
                        foreach (var error in updateResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                            _logger.LogWarning($"Failed to update user profile: {error.Description}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user profile");
                    TempData["Error"] = "Có lỗi xảy ra khi cập nhật thông tin. Vui lòng thử lại sau.";
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật thông tin. Vui lòng thử lại sau.");
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

        // Resend confirmation email (giữ lại để tương thích với Login view)
        [HttpPost]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] Dictionary<string, string>? request)
        {
            // Chuyển hướng sang ResendVerificationCode
            return await ResendVerificationCode(request);
        }

        // GET: /Account/VerifyCode
        [HttpGet]
        public IActionResult VerifyCode(string? email)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Email không hợp lệ.";
                return RedirectToAction("Register");
            }

            ViewBag.Email = email;
            return View();
        }

        // POST: /Account/VerifyCode
        [HttpPost]
        public async Task<IActionResult> VerifyCode(string email, string code)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ email và mã xác nhận.");
                ViewBag.Email = email;
                return View();
            }

            // Xác thực mã
            var cacheEntry = _verificationCodeService.VerifyCode(email, code);
            if (cacheEntry == null)
            {
                ModelState.AddModelError("", "Mã xác nhận không đúng hoặc đã hết hạn. Vui lòng thử lại.");
                ViewBag.Email = email;
                return View();
            }

            // Tìm user
            var user = await _userManager.FindByIdAsync(cacheEntry.UserId);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản. Vui lòng đăng ký lại.");
                return RedirectToAction("Register");
            }

            // Xác nhận email
            user.EmailConfirmed = true;
            // Đảm bảo UpdatedAt là UTC (PostgreSQL yêu cầu)
            user.UpdatedAt = DateTime.UtcNow;
            // Đảm bảo CreatedAt là UTC nếu có
            if (user.CreatedAt.HasValue && user.CreatedAt.Value.Kind != DateTimeKind.Utc)
            {
                user.CreatedAt = user.CreatedAt.Value.ToUniversalTime();
            }
            // Đảm bảo DateOfBirth là UTC nếu có
            if (user.DateOfBirth.HasValue && user.DateOfBirth.Value.Kind != DateTimeKind.Utc)
            {
                // DateOfBirth thường là local date, chỉ lấy date part
                user.DateOfBirth = user.DateOfBirth.Value.Date;
            }
            
            var updateResult = await _userManager.UpdateAsync(user);
            
            if (updateResult.Succeeded)
            {
                TempData["Message"] = "Email đã được xác nhận thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Login");
            }
            else
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi xác nhận email. Vui lòng thử lại.");
                ViewBag.Email = email;
                return View();
            }
        }

        // POST: /Account/ResendVerificationCode
        [HttpPost]
        public async Task<IActionResult> ResendVerificationCode([FromBody] Dictionary<string, string>? request)
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

            try
            {
                // Xóa mã cũ nếu có
                _verificationCodeService.RemoveCode(email);

                // Tạo mã mới
                var verificationCode = _verificationCodeService.GenerateCode();
                _verificationCodeService.StoreCode(email, verificationCode, user.Id);

                // Gửi email
                var emailSent = await _emailService.SendVerificationCodeAsync(email, verificationCode);
                _logger.LogInformation($"Resend verification code to {email}: {emailSent}");

                if (emailSent)
                {
                    return Json(new { success = true, message = "Mã xác nhận đã được gửi lại. Vui lòng kiểm tra hộp thư (có thể trong thư mục Spam)." });
                }
                else
                {
                    return Json(new { success = false, message = "Không thể gửi email. Vui lòng kiểm tra cấu hình email service hoặc thử lại sau." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending verification code");
                return Json(new { success = false, message = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau." });
            }
        }
    }
}
