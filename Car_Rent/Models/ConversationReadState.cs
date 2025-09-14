namespace Car_Rent.Models
{
    public class ConversationReadState
    {
        public int ConversationId { get; set; }
        public int UserId { get; set; }
        public long LastReadMessageId { get; set; }
    }
}
