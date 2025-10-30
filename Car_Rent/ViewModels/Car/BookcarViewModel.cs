using Car_Rent.Models;

namespace Car_Rent.ViewModels.Car
{
    public class BookcarViewModel
    {
        public List<Category> Categories { get; set; } = new();
        public List<CarOptionVM> Cars { get; set; } = new();

        public int? SelectedCategoryId { get; set; }
        public int? SelectedCarId { get; set; }
    }

    public class CarOptionVM
    {
        public int CarId { get; set; }
        public string CarName { get; set; } = "";
        public string LicensePlate { get; set; } = "";
    }

    public class LocationOptionVM
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = "";
        public int TenantId { get; set; }
        public string? TenantName { get; set; }
    }
}
