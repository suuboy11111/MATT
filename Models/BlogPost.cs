namespace MaiAmTinhThuong.Models
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        // Khóa ngoại từ ApplicationUser (tác giả bài viết)
        public string? AuthorId { get; set; }
        public ApplicationUser Author { get; set; }

        public string? ImageUrl { get; set; } // Để có thể nhận giá trị null

        // Thêm thuộc tính để đếm số lượt like
        public int LikeCount { get; set; }

        // Thêm thuộc tính này để kiểm tra xem người dùng đã like bài viết này chưa
        public bool LikedByUser { get; set; }

        // Mối quan hệ với bình luận
        public ICollection<Comment> Comments { get; set; }  // Liên kết với bảng Comment

        public bool IsApproved { get; set; }  // Để xác định bài viết đã được duyệt chưa
    }
}
