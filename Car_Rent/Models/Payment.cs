using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Car_Rent.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int ReservationId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    [Required]
    [RegularExpression("Unpaid|Paid", ErrorMessage = "Status is invalid")]
    public string Status { get; set; } = "Pending";

    public virtual Reservation? Reservation { get; set; } = null!;
}
