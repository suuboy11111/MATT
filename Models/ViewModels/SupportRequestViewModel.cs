using System.ComponentModel.DataAnnotations;
using MaiAmTinhThuong.Models.Enums;

namespace MaiAmTinhThuong.Models.ViewModels
{
    public class SupportRequestViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Tuổi không được để trống")]
        [Range(0, 150, ErrorMessage = "Tuổi phải từ 0 đến 150")]
        [Display(Name = "Tuổi")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [Display(Name = "Giới tính")]
        public Gender Gender { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ. Ví dụ: 0912345678 hoặc +84912345678")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [RegularExpression(@"^[0-9]{12}$", ErrorMessage = "Số CCCD phải có 12 chữ số")]
        [Display(Name = "Số CCCD")]
        public string? CCCD { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? ImageUrl { get; set; }

        [StringLength(1000, ErrorMessage = "Hoàn cảnh không được vượt quá 1000 ký tự")]
        [Display(Name = "Hoàn cảnh")]
        public string? Reason { get; set; }

        [StringLength(1000, ErrorMessage = "Tình trạng sức khỏe không được vượt quá 1000 ký tự")]
        [Display(Name = "Tình trạng sức khỏe")]
        public string? HealthStatus { get; set; }

        [Display(Name = "Mái ấm")]
        public int? MaiAmId { get; set; }
    }
}

