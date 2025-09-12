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
        }

        // Typing indicator (don gian)
        public async Task Typing(int conversationId)
        {
            var userId = GetUserId();
            if (!await _chat.IsParticipantAsync(conversationId, userId)) return;

            await Clients.Group(GroupName(conversationId)).SendAsync("SomeoneTyping", new { userId, conversationId });
        }

    }
}
