using Microsoft.AspNetCore.Identity;
using MaiAmTinhThuong.Models;  // Đảm bảo bạn sử dụng đúng namespace

namespace MaiAmTinhThuong.Extensions
{
    public static class IdentityExtensions
    {
        // Thêm extension cho việc lấy thông tin ProfilePicture từ ApplicationUser
        public static string GetProfilePicture(this ApplicationUser user)
        {
            return string.IsNullOrEmpty(user.ProfilePicture) ? "/images/default-avatar.png" : user.ProfilePicture;
        }
    }
}
