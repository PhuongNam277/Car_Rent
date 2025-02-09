using System;
using System.Collections.Generic;

namespace Car_Rent.Models;

public partial class MaintenanceRecord
{
    public int RecordId { get; set; }

    public int CarId { get; set; }

    public DateTime? MaintenanceDate { get; set; }

    public string? Description { get; set; }

    public decimal? Cost { get; set; }

    public virtual Car Car { get; set; } = null!;
}
