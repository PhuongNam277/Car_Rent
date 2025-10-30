using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rent.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int ReservationId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    [Required]
    [RegularExpression("Pending|Unpaid|Paid", ErrorMessage = "Status is invalid")]
    public string Status { get; set; } = "Pending";

    public int TenantId { get; set; }
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }
    public virtual Reservation? Reservation { get; set; } = null!;
}
