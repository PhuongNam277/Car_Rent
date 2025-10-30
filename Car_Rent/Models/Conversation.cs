namespace Car_Rent.Models
{
    public class Conversation
    {
        public int ConversationId { get; set; }
        public int CustomerId { get; set; }
        public int? StaffId { get; set; } // Nullable to allow for conversations not yet assigned to staff
        public string Status { get; set; } = "Open"; // e.g., "Open", "In Progress", "Closed"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClosedAt { get; set; }
        public int? TenantId { get; set; }


        public virtual User? Customer { get; set; } = null!;
        public virtual User? Staff { get; set; }
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public virtual Tenant? Tenant { get; set; }





    }
}
