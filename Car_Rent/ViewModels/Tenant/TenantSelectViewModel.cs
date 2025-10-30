namespace Car_Rent.ViewModels.Tenant
{
    public class TenantSelectViewModel
    {
        public string ReturnUrl { get; set; } = "/";
        public List<TenantItem> Tenants { get; set; } = new();
        public string? CurrentTenantId { get; set; }
    }

    public class TenantItem
    {
        public int TenantId { get; set; }
        public string Name { get; set; } = default!;
        public string Role { get; set; } = default!;
    }

    public class TenantCreateViewModel
    {
        public string ReturnUrl { get; set; } = "/";
        public string Name { get; set; } = default!;
    }
}
