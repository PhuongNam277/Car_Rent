using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rent.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int CarId { get; set; }

    public int UserId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime? ReviewDate { get; set; }

    public string? JobName { get; set; }

    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public virtual Car? Car { get; set; } = null!;

    public virtual User? User { get; set; } = null!;
}
