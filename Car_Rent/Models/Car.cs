using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rent.Models;

public partial class Car
{
    [Key]
    public int CarId { get; set; }
    
    [Required, MaxLength(100)]
    public string CarName { get; set; } = null!;

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

    [Required, MaxLength(20)]
    public string LicensePlate { get; set; } = null!;

    public int CategoryId { get; set; }

    public string? ImageUrl { get; set; }

    public decimal RentalPricePerDay { get; set; }

    public string? Status { get; set; }

    public int? SeatNumber { get; set; }

    public string? EnergyType { get; set; }

    public int? SellDate { get; set; }

    public string? EngineType { get; set; }

    public int? DistanceTraveled { get; set; }

    [MaxLength(20)]
    public string? TransmissionType { get; set; }

    [Required, MaxLength(20)]
    public string VehicleType { get; set; } = "Car"; // Car | Motorbike-Gas | ...
    public int BaseLocationId { get; set; }

    public virtual Location? BaseLocation { get; set; } = null!;

    public virtual Category? Category { get; set; } = null!;

    public virtual ICollection<MaintenanceRecord>? MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    public virtual ICollection<Reservation>? Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<Review>? Reviews { get; set; } = new List<Review>();

    // Multi-tenant
    public int TenantId { get; set; }
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }


}
