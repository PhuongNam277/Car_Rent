namespace Car_Rent.Models
{
    public class ChatMessage
    {
        public long ChatMessageId { get; set; }
        public int ConversationId { get; set; }
        public int SenderId { get; set; } // UserId of the sender
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        public virtual Conversation Conversation { get; set; } = null!;
        public virtual User Sender { get; set; } = null!;
    }
}
