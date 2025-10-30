namespace Car_Rent.Models
{
    public class Tenant
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = default!;
        public byte Status { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TenantMemberships> Memberships { get; set; }
    }
}
