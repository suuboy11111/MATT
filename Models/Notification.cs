using System.ComponentModel.DataAnnotations;

namespace MaiAmTinhThuong.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [StringLength(1000)]
        [Display(Name = "Nội dung")]
        public string? Message { get; set; }

        [Display(Name = "Loại thông báo")]
        public string Type { get; set; } // Info, Success, Warning, Error

        [Display(Name = "Người nhận")]
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        [Display(Name = "Đã đọc")]
        public bool IsRead { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Link")]
        public string? Link { get; set; }
    }
}






