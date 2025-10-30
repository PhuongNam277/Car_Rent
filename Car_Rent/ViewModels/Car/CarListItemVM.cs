namespace Car_Rent.ViewModels.Car
{
    public class CarListItemVM
    {
        public int CarId { get; set; }
        public string CarName { get; set; } = default!;
        public string Brand { get; set; } = default!;
        public string? ImageUrl { get; set; }
        public decimal RentalPricePerDay { get; set; }
        public int? SeatNumber { get; set; }
        public string? EnergyType { get; set; }
        public string? EngineType {  get; set; }
        public string? TransmissionType { get; set; }
        public string Status { get; set; } = "Available";
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int TenantId { get; set; }
    }
}
