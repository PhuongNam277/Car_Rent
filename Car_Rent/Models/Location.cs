using System.ComponentModel.DataAnnotations.Schema;

namespace Car_Rent.Models
{
    public partial class Location
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public string? TimeZone { get; set; }
        public bool IsActive { get; set; }
        public int TenantId { get; set; }
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }

        public virtual ICollection<Car> Cars { get; set; } = new List<Car>();
        public virtual ICollection<Reservation> PickupReservations { get; set; } = new List<Reservation>();
        public virtual ICollection<Reservation> DropoffReservations { get; set; } = new List<Reservation>();
    }
}
