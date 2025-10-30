using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
namespace Car_Rent.Models;

public partial class User
{
    public int UserId { get; set; }

    public string FullName { get; set; } = null!;

    [Required]
    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    [Required]

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public int? RoleId { get; set; }  // Nullable nếu có thể chưa gán

    public virtual Role? Role { get; set; }  // Navigation property

    public DateTime? CreatedDate { get; set; }

    public bool IsBlocked { get; set; } = false;

    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<TenantMemberships> Memberships { get; set; }
}
