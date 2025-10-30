using System.ComponentModel.DataAnnotations;

namespace Car_Rent.DTOs
{
    public class ReserveRequest
    {
        public int? CategoryId { get; set; }
        [Required] public int CarId { get; set; }
        [Required] public int TenantId { get;set; }

        [Required] public int PickupLocationId { get; set; }
        [Required] public int DropoffLocationId { get; set; }


        // Giữ tạm cho tương thích UI cũ (KHÔNG dùng trong logic)
        [Obsolete("Đã chuyển sang station-based.")]
        public string FromCity { get; set; } = string.Empty;
        [Obsolete("Đã chuyển sang station-based.")]
        public string ToCity { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Required, RegularExpression(@"^\d{2}:\d{2}$")]
        public string StartTime { get; set; } = "12:00";

        [Required, RegularExpression(@"^\d{2}:\d{2}$")]
        public string EndTime { get; set; } = "12:00";

        public decimal TotalPrice { get; set; }

        public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
        public bool IsOneWay => PickupLocationId != DropoffLocationId;
    }
}
