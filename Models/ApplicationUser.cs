using Microsoft.AspNetCore.Identity;

namespace MaiAmTinhThuong.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } // hoặc chỉ Tên
        public string Role { get; set; } // Admin, NguoiHoTro, NguoiCanHoTro

        public string ProfilePicture { get; set; } // Đường dẫn ảnh đại diện

    }
}
