using System.ComponentModel.DataAnnotations;

namespace MaiAmTinhThuong.Models
{
    public enum MatchType
    {
        Exact = 0,
        Contains = 1,
        Regex = 2
    }

    public class BotRule
    {
        public int Id { get; set; }

        [Required, StringLength(500)]
        public string Trigger { get; set; } = "";

        [Required]
        public MatchType MatchType { get; set; } = MatchType.Contains;

        [Required, StringLength(2000)]
        public string Response { get; set; } = "";

        public bool IsActive { get; set; } = true;

        // Priority: số càng cao => ưu tiên càng trước
        public int Priority { get; set; } = 0;
    }
}
