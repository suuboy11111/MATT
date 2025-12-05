namespace MaiAmTinhThuong.Models
{
    public class Supporter
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string? CCCD { get; set; }
        public string ImageUrl { get; set; }

        // Thêm MaiAmId vào Supporter
        public int? MaiAmId { get; set; }  // Chỉ chọn 1 mái ấm

        // Quan hệ 1-1 với MaiAm
        public virtual MaiAm MaiAm { get; set; }  // Liên kết mái ấm

        // Loại hỗ trợ có thể chọn nhiều loại (Tài chính, Vật tư, Chỗ ở)
        public ICollection<SupportType> SupportTypes { get; set; } = new List<SupportType>();
        public virtual ICollection<SupporterSupportType> SupporterSupportTypes { get; set; }

        public bool IsApproved { get; set; }       // Duyệt hồ sơ

        public virtual ICollection<SupporterMaiAm> SupporterMaiAms { get; set; } // Quan hệ nhiều mái ấm

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}