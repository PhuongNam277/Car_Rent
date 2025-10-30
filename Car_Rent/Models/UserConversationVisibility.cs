namespace Car_Rent.Models
{
    public class UserConversationVisibility
    {
        public int UserId { get; set; }
        public int ConversationId { get; set; }
        public bool IsHidden { get; set; }

        public virtual User User { get; set; } = null!;
        public virtual Conversation Conversation { get; set; } = null!;
    }
}
