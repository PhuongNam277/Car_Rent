namespace Car_Rent.Models
{
    public class TenantCategory
    {
        public int TenantId { get; set; }
        public int CategoryId { get; set; }
        public string? DisplayNameOverride { get; set; }
        public bool IsHidden { get; set; }
        public int? SortOrderOverride { get; set; }

        public Tenant Tenant { get; set; } = default!;
        public Category Category { get; set; } = default!;
    }
}
