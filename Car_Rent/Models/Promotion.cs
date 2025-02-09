using System;
using System.Collections.Generic;

namespace Car_Rent.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string Code { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? DiscountPercent { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Status { get; set; }
}
