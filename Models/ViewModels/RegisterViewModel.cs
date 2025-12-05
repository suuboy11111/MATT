using System.ComponentModel.DataAnnotations;

namespace MaiAmTinhThuong.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Loại tài khoản")]
        public string Role { get; set; } // Admin, NguoiCanHoTro, NguoiHoTro

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Xác nhận mật khẩu không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }
    }
}
