using System.ComponentModel.DataAnnotations;

namespace Car_Rent.Models
{
    public class TenantMemberships
    {
        [Key]
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public string Role { get; set; } = "Owner";

        // Nav
        public Tenant Tenant { get; set; } = default!;
        public User User { get; set; } = default!;
    }
}
