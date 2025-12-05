using MaiAmTinhThuong.Models;

public class Like
{
    public int Id { get; set; }
    public int BlogPostId { get; set; }
    public string UserId { get; set; }  // Khóa ngoại từ ApplicationUser

    public BlogPost BlogPost { get; set; }
    public ApplicationUser User { get; set; }  // Liên kết với người dùng
}