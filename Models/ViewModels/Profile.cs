namespace MaiAmTinhThuong.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string FullName { get; set; } // Tên đầy đủ
        public string ProfilePicture { get; set; } // Đường dẫn ảnh đại diện
        public string CurrentPassword { get; set; } // Mật khẩu hiện tại (dùng để thay đổi mật khẩu)
        public string NewPassword { get; set; } // Mật khẩu mới
        public string ConfirmPassword { get; set; } // Xác nhận mật khẩu mới
    }
}