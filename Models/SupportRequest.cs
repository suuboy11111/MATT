namespace MaiAmTinhThuong.Models
{
    public class SupportRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string CCCD { get; set; }
        public string ImageUrl { get; set; }
        public string Reason { get; set; }          // Hoàn cảnh, lý do cần hỗ trợ
        public string HealthStatus { get; set; }    // Tình trạng sức khỏe
        public bool IsApproved { get; set; }        // Duyệt hồ sơ
        public bool IsSupported { get; set; }       // Đang được hỗ trợ chung
        public bool IsSupportedReason { get; set; } // Hỗ trợ về hoàn cảnh
        public bool IsSupportedHealth { get; set; } // Hỗ trợ về sức khỏe

        // Sửa MaiAmId thành nullable để tránh lỗi khóa ngoại khi chưa có mái ấm liên kết
        public int? MaiAmId { get; set; }
        public virtual MaiAm MaiAm { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
