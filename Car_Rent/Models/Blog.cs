using System;
using System.Collections.Generic;

namespace Car_Rent.Models;

public partial class Blog
{
    public int BlogId { get; set; }

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public int AuthorId { get; set; }

    public DateTime? PublishedDate { get; set; }

    public string? Status { get; set; }

    public virtual User Author { get; set; } = null!;
}
