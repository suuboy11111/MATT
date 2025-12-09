using System;

namespace MaiAmTinhThuong.Models.ViewModels
{
    public class BlogPostViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AuthorName { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int TotalInteraction { get; set; }
    }
}









