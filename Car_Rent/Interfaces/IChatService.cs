using Car_Rent.Models;

namespace Car_Rent.Interfaces
{
    public interface IChatService
    {
        Task<Conversation> GetOrCreateConversationForCustomerAsync(int customerId);
        Task<Conversation?> GetConversationAsync(int conversationId);
        Task<bool> IsParticipantAsync(int conversationId, int userId);
        Task<ChatMessage> SaveMessageAsync(int conversationId, int senderId, string content);
        Task<List<ChatMessage>> GetRecentMessageAsync(int conversationId, int take = 50);
        Task<int?> PickStaffForCustomerAsync(int customerId); // Don gian, chon 1 staff neu co
    }
}
