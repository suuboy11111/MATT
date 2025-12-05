namespace MaiAmTinhThuong.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public int BlogPostId { get; set; }
        public string? AuthorId { get; set; }  // Khóa ngoại từ ApplicationUser
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        
        public BlogPost BlogPost { get; set; }  // Liên kết với bài viết
        public ApplicationUser Author { get; set; }  // Liên kết với người dùng
    }
}
