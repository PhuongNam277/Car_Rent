using System.Security.Claims;
using Car_Rent.Hubs;
using Car_Rent.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Car_Rent.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class ChatController : Controller
    {
        private readonly IChatService _chat;
        private readonly IHubContext<ChatHub> _hub;
        public ChatController(IChatService chat, IHubContext<ChatHub> hub)
        {
            _chat = chat;
            _hub = hub;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Khi bam "Chat Now" => Tao hoac lay conversation dang Open
        [HttpGet]
        [Authorize(AuthenticationSchemes = "MyCookieAuth")]
        public async Task<IActionResult> Start()
        {
            var customerId = GetUserId();
            var conv = await _chat.GetOrCreateConversationForCustomerAsync(customerId);
            // Neu chua co staff, thong bao len kenh truc ca cho tat ca staff online
            if (conv.StaffId == null)
            {
                // Khong co staff, thong bao len kenh lobby
                await _hub.Clients.Group("staff:looby").SendAsync("NewConversation", new
                {
                    conversationId = conv.ConversationId,
                    customerId,
                    customerName = User.Identity!.Name ?? ("User#" + customerId),
                    createdAt = DateTime.UtcNow
                });
                
            }
            else
            {
                // Da co staff gan & van Open: ping dung Staff (khach quay lai)
                await _hub.Clients.User(conv.StaffId.ToString()!)
                    .SendAsync("CustomerBack", new
                    {
                        conversationId = conv.ConversationId,
                        customerId,
                        customerName = User.Identity!.Name ?? ("User#" + customerId),
                        at = DateTime.UtcNow
                    });
            }

            return RedirectToAction(nameof(Room), new { id = conv.ConversationId });
        }

        // Phong chat (khach hoac staff deu vao duoc)
        [HttpGet]
        public async Task<IActionResult> Room(int id)
        {
            var userId = GetUserId();
            if(!await _chat.IsParticipantAsync(id, userId))
            {
                return Forbid();
            }

            var messages = await _chat.GetRecentMessageAsync(id, take: 50);
            ViewBag.ConversationId = id;
            ViewBag.CurrentUserId = userId;
            return View(messages);
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "MyCookieAuth", Roles = "Staff")]
        public async Task<IActionResult> Inbox()
        {
            var me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            ViewBag.Assigned = await _chat.GetAssignedOpenAsync(me); // da gan cho staff nay
            ViewBag.Queue = await _chat.GetOpenQueueAsync();
            return View();
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "MyCookieAuth", Roles = "Staff")]
        public async Task<IActionResult> QueueCount()
        {
            var list = await _chat.GetOpenQueueAsync();
            return Json(new { count = list.Count });
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "MyCookieAuth", Roles = "Staff")]
        public async Task<IActionResult> Counts()
        {
            var me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var queue = await _chat.GetOpenQueueAsync(); // Open + StaffId == null
            //var mine = await _chat.GetAssignedOpenAsync(me); // Open + StaffId == me
            var unread = await _chat.GetUnreadCountsForStaffAsync(me); // ConvId -> Count
            return Json(new
            {
                queueIds = queue.Select(c => c.ConversationId).ToArray(),
                //assignedIds = mine.Select(c => c.ConversationId).ToArray()
                unread = unread // Dict
            });
        }
    }
}
