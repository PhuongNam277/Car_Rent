namespace Car_Rent.ViewModels.Car
{
    public class CarIndexVM
    {
        public CarFilterVM Filters { get; set; } = new();
        public List<CarListItemVM> Cars { get; set; } = new();
        
        // Pagination
        public int TotalItems { get; set; }
        public int Page => Filters.Page;
        public int PageSize => Filters.PageSize;
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / Math.Max(1, PageSize));

        // id, name, count cho tabs category hiện có
        public List<(int CategoryId, string CategoryName, int Count)> Categories { get; set; } = new();

        public List<(int TenantId, string Name, int Count)> Tenants { get; set; } = new();
    }
}
