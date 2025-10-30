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

        public async Task<List<ChatMessage>> GetRecentMessageAsync(int conversationId, int take = 50)
        {
            var messages = await _context.ChatMessages
                            .AsNoTracking()
                            .Where(m => m.ConversationId == conversationId)
                            .OrderByDescending(m => m.SentAt)
                            .Take(take)
                            .ToListAsync();

            messages.Reverse();
            return messages;
        }
            



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

        public async Task MarkReadAsync(int conversationId, int userId, long lastMessageId)
        {
            var rs = await _context.Set<ConversationReadState>()
                .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == userId);

            if (rs == null)
            {
                rs = new ConversationReadState
                {
                    ConversationId = conversationId,
                    UserId = userId,
                    LastReadMessageId = lastMessageId
                };

                _context.ConversationReadStates.Add(rs);
            }
            else if (lastMessageId > rs.LastReadMessageId)
            {
                rs.LastReadMessageId = lastMessageId;
            }
            else
            {
                await _context.SaveChangesAsync();
                return;
            }

            // Cập nhật IsRead trong ChatMessages
            
        }

        public async Task<Dictionary<int, int>> GetUnreadCountsForStaffAsync(int staffId)
        {
            // Cac conv dang Open, gan cho staffId
            var convIds = await _context.Conversations
                .Where(c => c.Status == "Open" && c.StaffId == staffId)
                .Select(c => c.ConversationId)
                .ToListAsync();

            if (convIds.Count == 0) return new Dictionary<int, int>();

            // Lay LastRead cua tung staff cho tung conv
            var lastReads = await _context.Set<ConversationReadState>()
                .Where(r => r.UserId == staffId && convIds.Contains(r.ConversationId))
                .ToDictionaryAsync(r => r.ConversationId, r => r.LastReadMessageId);

            // Dem so message cua KHACH co Id > LastRead
            var q = _context.ChatMessages
                .Where(m => convIds.Contains(m.ConversationId))
                .GroupBy(m => m.ConversationId)
                .Select(g => new
                {
                    ConversationId = g.Key,
                    MaxId = g.Max(x => x.ChatMessageId)

                });

            var result = new Dictionary<int, int>();
            foreach (var cid in convIds)
            {
                var lastRead = lastReads.TryGetValue(cid, out var lr) ? lr : 0;
                var count = await _context.ChatMessages.CountAsync(m =>
                m.ConversationId == cid &&
                m.ChatMessageId > lastRead &&
                m.SenderId != staffId); // chi dem message cua KHACH
                result[cid] = count;
            }
            return result;
        }

        public async Task<long> GetLastMessageIdAsync(int conversationId)
        {
            var id = await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .Select(m => (long?)m.ChatMessageId)
                .OrderByDescending(x => x)
                .FirstOrDefaultAsync();
            return id ?? 0;
        }

        public async Task<Conversation> GetOrCreateConversationForCustomerAsync(int customerId, int tenantId)
        {
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.CustomerId == customerId
                                        && c.TenantId == tenantId
                                        && c.Status == "Open"
                );

            if( conversation == null )
            {
                conversation = new Conversation
                {
                    CustomerId = customerId,
                    TenantId = tenantId,
                    Status = "Open",
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow
                };
                _context.Conversations.Add( conversation );
                await _context.SaveChangesAsync();
            }
            return conversation;
        }

        public async Task<List<Conversation>> GetAllConversationsForCustomerAsync(int customerId)
        {
            // Lấy danh sách ID các cuộc hội thoại bị user ẩn đi
            var hiddenConvIds = await _context.UserConversationVisibilities
                .Where(v => v.UserId == customerId && v.IsHidden)
                .Select(v => v.ConversationId)
                .ToListAsync();

            return await _context.Conversations
                .AsNoTracking()
                .Where(c => c.CustomerId == customerId && !hiddenConvIds.Contains(c.ConversationId))
                .Include(c => c.Tenant)
                .Include(c => c.Staff)
                .Include(c => c.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        public async Task<List<Conversation>> GetOpenQueueForTenantAsync(int tenantId)
        {
            return await _context.Conversations
                .AsNoTracking()
                .Include(c => c.Tenant)
                .Where(c => c.Status == "Open" && c.StaffId == null && c.TenantId == tenantId)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
