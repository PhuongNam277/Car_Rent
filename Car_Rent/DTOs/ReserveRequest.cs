namespace Car_Rent.DTOs
{
    public class ReserveRequest
    {
        public int? CategoryId { get; set; }
        public int CarId { get; set; }
        public string FromCity { get; set; } = string.Empty;
        public string ToCity { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }
    }
}
