using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Car_Rent.Models;

[Index(nameof(BlogId))]
[Table("Comment")]
public partial class Comment
{
    [Key]
    public int CommentId { get; set; }

    public int BlogId { get; set; }

    public int UserId { get; set; }

    [StringLength(100)]
    public string AuthorName { get; set; } = null!;

    public string Content { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Add anonymous
    public bool IsAnonymous { get; set; }

    // Navigation property
    public Blog? Blog{ get; set; }
    public User? User{ get; set; }

}
