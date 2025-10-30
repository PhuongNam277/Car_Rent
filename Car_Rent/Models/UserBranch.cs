namespace Car_Rent.Models
{
    public class UserBranch
    {
        public int UserId { get; set; }
        public int TenantId { get; set; }
        public int LocationId { get; set; }

        public User User { get; set; } = default!;
        public Tenant Tenant { get; set; } = default!;
        public Location Location { get; set; } = default!;
    }
}
