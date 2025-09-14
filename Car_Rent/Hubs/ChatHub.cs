using System.Security.Claims;
using Car_Rent.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Car_Rent.Hubs
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class ChatHub : Hub
    {
        private readonly IChatService _chat;
        public ChatHub(IChatService chat)
        {
            _chat = chat;
        }

        public int GetUserId() => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GroupName(int convId) => $"conv:{convId}";

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            // Neu la staff : tu join lobby + seed danh sach
            if (Context.User?.IsInRole("Staff") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "staff:lobby");

                var openUnassigned = await _chat.GetOpenQueueAsync();
                await Clients.Caller.SendAsync("SeedQueue", openUnassigned.Select(c => new
                {
                    conversationId = c.ConversationId,
                    customerId = c.CustomerId,
                    customerName = c.Customer?.Username,
                    createdAt = c.CreatedAt
                }));

                // Seed cac cuon chat Open da gan cho Staff hien tai
                var staffId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var mine = await _chat.GetAssignedOpenAsync(staffId);
                await Clients.Caller.SendAsync("SeedAssigned", mine.Select(c => new
                {
                    conversationId = c.ConversationId,
                    customerId = c.CustomerId,
                    customerName = c.Customer?.Username,
                    lastMessageAt = c.LastMessageAt
                }));
            }
        }

        // Client goi ngay khi vao phong de join group
        public async Task JoinConversation(int conversationId)
        {
            var userId = GetUserId();
            if (!await _chat.IsParticipantAsync(conversationId, userId))
                throw new HubException("Not allowed to join this conversation.");

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
        }

        // Gui tin
        public async Task SendMessage(int conversationId, string content)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(content)) return;

            if (!await _chat.IsParticipantAsync(conversationId, userId))
                throw new HubException("Not allowed");

            var msg = await _chat.SaveMessageAsync(conversationId, userId, content);
            await Clients.Group(GroupName(conversationId)).SendAsync("ReceiveMessage", new
            {
                messageId = msg.ChatMessageId,
                conversationId,
                senderId = msg.SenderId,
                content = msg.Content,
                sentAt = msg.SentAt
            });

            // neu nguoi gui la khach va conv da gan staff -> notify staff "New Message"
            var conv = await _chat.GetConversationAsync(conversationId);
            if (conv?.StaffId != null && conv.CustomerId == userId)
            {
                await Clients.User(conv.StaffId.Value.ToString())
                    .SendAsync("NewMessage", new
                    {
                        conversationId,
                        messageId = msg.ChatMessageId,
                        preview = msg.Content.Length > 60 ? msg.Content.Substring(0, 60) + "..." : msg.Content,
                        at = msg.SentAt
                    });
            }
        }

        // Staff hoac Customer hoi khi mo/doc phong
        public async Task MarkRead(int conversationId, long lastMessageId)
        {
            var userId = GetUserId();
            if (!await _chat.IsParticipantAsync(conversationId, userId)) return;

            await _chat.MarkReadAsync(conversationId, userId, lastMessageId);
        }

        // Typing indicator (don gian)
        public async Task Typing(int conversationId)
        {
            var userId = GetUserId();
            if (!await _chat.IsParticipantAsync(conversationId, userId)) return;

            await Clients.Group(GroupName(conversationId)).SendAsync("SomeoneTyping", new { userId, conversationId });
        }

        [Authorize(AuthenticationSchemes = "MyCookieAuth", Roles = "Staff")]
        public async Task JoinStaffLobby()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff:lobby");
        }

        [Authorize(AuthenticationSchemes = "MyCookieAuth", Roles = "Staff")]
        public async Task<bool> AcceptConversation(int conversationId)
        {
            var staffId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _chat.TryAssignStaffAsync(conversationId, staffId);

            if (ok)
            {
                // Bao cho tat ca staff trong lobby biet: cuoc chat nay da co nguoi nhan
                await Clients.Group("staff:lobby").SendAsync("ConversationAssigned", new { conversationId, staffId });

                // Bao rieng cho nguoi vua nhan: mo phong chat
                await Clients.Caller.SendAsync("OpenConversation", new { conversationId });
            }
            return ok;
        }

    }
}
