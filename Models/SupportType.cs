namespace MaiAmTinhThuong.Models
{
    public class SupportType
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public virtual ICollection<SupporterSupportType> SupporterSupportTypes { get; set; }
    }
}