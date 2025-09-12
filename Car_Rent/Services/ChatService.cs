using Car_Rent.Interfaces;
using Car_Rent.Models;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Services
{
    public class ChatService : IChatService
    {
        private readonly CarRentalDbContext _context;
        public ChatService(CarRentalDbContext context)
        {
            _context = context;
        }

        public async Task<Conversation> GetOrCreateConversationForCustomerAsync(int customerId)
        {
            var conv = await _context.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "Open");

            if (conv != null) return conv;

            var staffId = await PickStaffForCustomerAsync(customerId); // co the null
            conv = new Conversation
            {
                CustomerId = customerId,
                StaffId = null,
                Status = "Open",
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conv);
            await _context.SaveChangesAsync();
            return conv;
        }

        public async Task<bool> TryAssignStaffAsync(int conversationId, int staffId)
        {
            // Cap nhat co dieuf kien de chong tranh chap 2 staff cung luc
            var rows = await _context.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.Conversations SET StaffId = {0} WHERE ConversationId = {1} AND StaffId IS NULL",
                 staffId, conversationId);

            return rows == 1;
                
        }

        public Task<List<Conversation>> GetOpenQueueAsync()
        {
            return _context.Conversations
                .Include(c => c.Customer)
                .Where(c => c.Status == "Open" && c.StaffId == null)
                .OrderByDescending(c => c.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        public Task<Conversation?> GetConversationAsync(int conversationId)
            => _context.Conversations.FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        public async Task<bool> IsParticipantAsync(int conversationId, int userId)
        {
            var conv = await _context.Conversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conv == null) return false;
            return conv.CustomerId == userId || conv.StaffId == userId;
        }

        public async Task<ChatMessage> SaveMessageAsync(int conversationId, int senderId, string content)
        {
            var msg = new ChatMessage
            {
                ConversationId = conversationId,
                SenderId = senderId,
                Content = content.Trim(),
                SentAt = DateTime.UtcNow,
            };

            _context.ChatMessages.Add(msg);

            var conv = await _context.Conversations.FindAsync(conversationId);
            if (conv != null)
            {
                conv.LastMessageAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return msg;
        }

        public Task<List<ChatMessage>> GetRecentMessageAsync(int conversationId, int take = 50)
            => _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.ConversationId == conversationId)
            .Take(take)
            .OrderBy(m => m.ChatMessageId)
            .ToListAsync();

        public async Task<int?> PickStaffForCustomerAsync(int customerId)
        {
            // don gian: lay 1 username co RoleName = "Staff", nang cap round-robin later
            var staffId = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role != null && u.Role.RoleName == "Staff" && !u.IsBlocked)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync();

            return staffId; // co the null
        }

        public Task<List<Conversation>> GetAssignedOpenAsync(int staffId)
        {
            return _context.Conversations
                .Include(c => c.Customer)
                .Where(c => c.Status == "Open" && c.StaffId == staffId)
                .OrderByDescending(c => c.LastMessageAt)
                .Take(100)
                .ToListAsync();
        }
    }
}
