namespace MaiAmTinhThuong.Models
{
    public class SupporterMaiAm
    {
        public int SupporterId { get; set; }
        public virtual Supporter Supporter { get; set; }

        public int MaiAmId { get; set; }
        public virtual MaiAm MaiAm { get; set; }
    }
}
