namespace Car_Rent.ViewModels.Car
{
    public class CarFilterVM
    {
        public int? CategoryId { get; set; }
        public decimal? PriceMin { get; set; }
        public decimal? PriceMax { get; set; }
        public int? Seats { get; set; }
        public string? Transmission {  get; set; } // "Automatic"/"Manual"
        public string? Energy { get; set; } // Gasoline/Diesel/Hybrid/Electric
        public string SortBy { get; set; } = "PriceAsc"; // PriceAsc|PriceDesc|NameAsc|NameDesc|Newest
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;

        public int? TenantId { get; set; }
        public string? VehicleType { get; set; }

        // helper để giữ query khi phân trang/sort
        public RouteValueDictionary ToRouteValues()
        {
            var r = new RouteValueDictionary
            {
                ["categoryId"] = CategoryId,
                ["priceMin"] = PriceMin,
                ["priceMax"] = PriceMax,
                ["seats"] = Seats,
                ["transmission"] = Transmission,
                ["energy"] = Energy,
                ["sortBy"] = SortBy,
                ["page"] = Page,
                ["pageSize"] = PageSize,
            };
            r["tenantId"] = TenantId;
            r["vehicleType"] = VehicleType;
            return r;
        }
    }
}
