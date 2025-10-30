using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rent.Models;

public partial class Reservation
{
    public int ReservationId { get; set; }

    public int UserId { get; set; }

    public int CarId { get; set; }

    public DateTime? ReservationDate { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public decimal TotalPrice { get; set; }

    [Required]
    [RegularExpression("Pending|Confirmed|Cancelled|Completed|Rejected|InProgress", ErrorMessage = "Status is invalid")]
    public string? Status { get; set; } = "Pending";

    public string? FromCity { get; set; }
    public string? ToCity { get; set; }


    public virtual Car? Car { get; set; } = null!;

    public virtual ICollection<Payment>? Payments { get; set; } = new List<Payment>();

    public virtual User? User { get; set; } = null!;

    // New
    public int? PickupLocationId { get; set; }
    public int? DropoffLocationId { get; set; }

    public int TenantId { get; set; }

    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public virtual Location? PickupLocation { get; set; } = null!;
    public virtual Location? DropoffLocation { get; set; } = null!;



}
