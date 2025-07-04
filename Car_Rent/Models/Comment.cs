using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Models;

[Table("Comment")]
public partial class Comment
{
    [Key]
    public int CommentId { get; set; }

    public int BlogId { get; set; }

    [StringLength(100)]
    public string AuthorName { get; set; } = null!;

    public string Content { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

}
