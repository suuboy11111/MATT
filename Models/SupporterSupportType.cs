namespace MaiAmTinhThuong.Models
{
    public class SupporterSupportType
    {
        public int SupporterId { get; set; }
        public virtual Supporter Supporter { get; set; }

        public int SupportTypeId { get; set; }
        public virtual SupportType SupportType { get; set; }
    }
}
