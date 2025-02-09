using System;
using System.Collections.Generic;

namespace Car_Rent.Models;

public partial class Car
{
    public int CarId { get; set; }

    public string CarName { get; set; } = null!;

    public string Brand { get; set; } = null!;

    public string Model { get; set; } = null!;

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

    public string? TransmissionType { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
