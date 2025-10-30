using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Car_Rent.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    [Required, MaxLength(200)]
    public string CategoryName { get; set; } = null!;

    [Required, MaxLength(200)]
    public string VehicleType { get; set; } = "Car";

    public int? ParentCategoryId { get; set; }

    [Required, MaxLength(120)]
    public string? Slug { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    // Navs
    public Category? ParentCategory { get; set; }
    public virtual ICollection<Category>? SubCategories { get; set; } = new List<Category>();

    public virtual ICollection<Car>? Cars { get; set; } = new List<Car>();
}
