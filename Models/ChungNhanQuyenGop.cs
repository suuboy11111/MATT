using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MaiAmTinhThuong.Models
{
    public class ChungNhanQuyenGop
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VinhDanhId { get; set; }

        [ForeignKey("VinhDanhId")]
        public VinhDanh VinhDanh { get; set; }

        [Required]
        public DateTime NgayCap { get; set; } = DateTime.UtcNow;

        [Required]
        public string SoChungNhan { get; set; } // Ví dụ: CN-2025-001

        public string NoiDung { get; set; } // ví dụ: "Đã ủng hộ 5,000,000 VNĐ cho Mái ấm Tình Thương"

        [Required]
        public string FilePath { get; set; } = string.Empty;
    }
}
