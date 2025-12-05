namespace MaiAmTinhThuong.Models
{
    public class MaiAm
    {
        public int Id { get; set; }
        public string Name { get; set; }            // Tên mái ấm
        public string Description { get; set; }     // Mô tả mái ấm
        public string Address { get; set; }         // Địa chỉ mái ấm
        public decimal Fund { get; set; }            // Quỹ tài trợ hiện tại
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        // Navigation properties
        public virtual ICollection<SupportRequest> SupportRequests { get; set; }
        public virtual ICollection<SupporterMaiAm> SupporterMaiAms { get; set; }
        public virtual ICollection<Supporter> Supporters { get; set; }

        public MaiAm()
        {
            SupportRequests = new HashSet<SupportRequest>();
            SupporterMaiAms = new HashSet<SupporterMaiAm>();
            Supporters = new HashSet<Supporter>();
        }
    }
}
