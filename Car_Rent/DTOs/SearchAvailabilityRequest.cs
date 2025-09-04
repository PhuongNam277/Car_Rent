using System.ComponentModel.DataAnnotations;

namespace Car_Rent.DTOs
{
    public class SearchAvailabilityRequest
    {
        [Required] public int PickupLocationId { get; set; }
        public int? CategoryId { get; set; }

        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }

        [Required, RegularExpression(@"^\d{2}:\d{2}$")]
        public string StartTime { get; set; } = "12:00";

        [Required, RegularExpression(@"^\d{2}:\d{2}$")]
        public string EndTime { get; set; } = "12:00";
    }
}
