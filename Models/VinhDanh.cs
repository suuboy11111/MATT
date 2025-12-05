using System.ComponentModel.DataAnnotations;

namespace MaiAmTinhThuong.Models
{
    public class VinhDanh
    {
        [Key]
        public int Id { get; set; } 
        [Required]
        public String HoTen { get; set; }
        [Required]
        public string Loai { get; set; }//NHT or TNV
        public decimal? SoTienUngHo { get; set; } // nếu là nhà hảo tâm
        public int? SoGioHoatDong { get; set; } // nếu là tình nguyện viên
        public DateTime NgayVinhDanh { get; set; } = DateTime.Now;
        public string GhiChu { get; set; }
    }

}
