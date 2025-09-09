using System.ComponentModel.DataAnnotations;
using Car_Rent.Models;
namespace Car_Rent.ViewModels.Blog
{
    public class BlogDetailsViewModel
    {
        public Car_Rent.Models.Blog Blog { get; set; } = default!;
        public List<Comment> Comments { get; set; } = new List<Comment>();

        public BlogCommentInput NewComment { get; set; } = new BlogCommentInput();
    }

    public class BlogCommentInput
    {
        [Required]
        public int BlogId { get; set; }

        [Required, StringLength(2000, MinimumLength = 3)]
        public string Content { get; set; } = string.Empty;

        public bool IsAnonymous { get; set; }
    }

    public class CommentEditInput
    {
        [Required]
        public int CommentId { get; set; }

        [Required]
        public int BlogId { get; set; }

        [Required, StringLength(2000, MinimumLength = 3)]
        public string Content { get; set; } = string.Empty;
    }
}
