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

        // helper để giữ query khi phân trang/sort
        public Dictionary<string, string?> ToRouteValues()
            => new()
            {
                ["categoryId"] = CategoryId?.ToString(),
                ["priceMin"] = PriceMin?.ToString(),
                ["priceMax"] = PriceMax?.ToString(),
                ["seats"] = Seats?.ToString(),
                ["transmission"] = Transmission,
                ["energy"] = Energy,
                ["sortBy"] = SortBy
            };
    }
}
