namespace Car_Rent.DTOs
{
    public class CarDto
    {
        public int CarId { get; set; }
        public required string CarName { get; set; }
        public required string Model { get; set; }
        public required string CategoryName { get; set; }
        public required string LicensePlate { get; set; }
        public required decimal RentalPricePerDay { get; set; }
        public required int CategoryId { get; set; }
        public required string Brand { get; set; }
        public required int BaseLocationId { get; set; }
    }
}
