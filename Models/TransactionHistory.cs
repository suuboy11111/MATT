using System;

namespace MaiAmTinhThuong.Models
{
    public class TransactionHistory
    {
        public int Id { get; set; }

        public int MaiAmId { get; set; }
        public virtual MaiAm MaiAm { get; set; }

        public int? SupporterId { get; set; }  // Người tài trợ, có thể null nếu không rõ
        public virtual Supporter Supporter { get; set; }

        public decimal Amount { get; set; }    // Số tiền giao dịch

        public DateTime TransactionDate { get; set; }

        public string Status { get; set; }     // Trạng thái: "Success", "Failed",...

        public string Description { get; set; }  // Mô tả giao dịch (ví dụ: "Ủng hộ tài chính")
    }
}
