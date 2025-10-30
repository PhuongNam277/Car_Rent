using System.Security.Claims;
using Car_Rent.Hubs;
using Car_Rent.Interfaces;
using Car_Rent.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class ChatController : Controller
    {
        private readonly IChatService _chat;
        private readonly IHubContext<ChatHub> _hub;
        private readonly CarRentalDbContext _context;
        public ChatController(IChatService chat, IHubContext<ChatHub> hub, CarRentalDbContext context)
        {
            _chat = chat;
            _hub = hub;
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        //// Khi bam "Chat Now" => Tao hoac lay conversation dang Open
        //[HttpGet]
        //[Authorize(AuthenticationSchemes = "MyCookieAuth")]
        //public async Task<IActionResult> Start()
        //{
        //    var customerId = GetUserId();
        //    var conv = await _chat.GetOrCreateConversationForCustomerAsync(customerId);
        //    // Neu chua co staff, thong bao len kenh truc ca cho tat ca staff online
        //    if (conv.StaffId == null)
        //    {
        //        // Khong co staff, thong bao len kenh lobby
        //        await _hub.Clients.Group("staff:looby").SendAsync("NewConversation", new
        //        {
        //            conversationId = conv.ConversationId,
        //            customerId,
        //            customerName = User.Identity!.Name ?? ("User#" + customerId),
        //            createdAt = DateTime.UtcNow
        //        });
                
        //    }
        //    else
        //    {
        //        // Da co staff gan & van Open: ping dung Staff (khach quay lai)
        //        await _hub.Clients.User(conv.StaffId.ToString()!)
        //            .SendAsync("CustomerBack", new
        //            {
        //                conversationId = conv.ConversationId,
        //                customerId,
        //                customerName = User.Identity!.Name ?? ("User#" + customerId),
        //                at = DateTime.UtcNow
        //            });
        //    }

        //    return RedirectToAction(nameof(Room), new { id = conv.ConversationId });
        //}

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
            try // <--- Thêm try-catch để debug dễ hơn
            {
                // Kiểm tra dòng này: Có thể User hoặc ClaimTypes.NameIdentifier bị null?
                var me = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Kiểm tra các phương thức service này:
                var queue = await _chat.GetOpenQueueAsync(); // Có lỗi bên trong không?
                var unread = await _chat.GetUnreadCountsForStaffAsync(me); // Có lỗi bên trong không?

                return Json(new
                {
                    queueIds = queue?.Select(c => c.ConversationId).ToArray() ?? Array.Empty<int>(), // Thêm kiểm tra null
                    unread = unread ?? new Dictionary<int, int>() // Thêm kiểm tra null
                });
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết ra console server
                Console.WriteLine($"Error in Chat/Counts: {ex.ToString()}");
                // Trả về lỗi 500 với thông báo (chỉ khi debug)
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "MyCookieAuth")]
        public async Task<IActionResult> StartWithTenant(int tenantId)
        {
            var customerId = GetUserId();

            // Gọi phương thức mới trong service
            var conv = await _chat.GetOrCreateConversationForCustomerAsync(customerId, tenantId);

            // Thông báo cho staff thuộc tenant đó
            if (conv.Staff == null)
            {
                await _hub.Clients.Group($"staff:tenant:{tenantId}").SendAsync("NewConversation", new
                {
                    conversationId = conv.ConversationId,
                    customerId,
                    customerName = User.Identity!.Name ?? ("User#" + customerId),
                    createdAt = DateTime.UtcNow
                });
            }
            else {
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

            return RedirectToAction(nameof(Room), new {id = conv.ConversationId});

        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "MyCookieAuth")]
        public async Task<IActionResult> History()
        {
            var customerId = GetUserId();
            var conversations = await _chat.GetAllConversationsForCustomerAsync(customerId);
            return View(conversations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "MyCookieAuth")]
        public async Task<IActionResult> HideConversation(int id)
        {
            var userId = GetUserId();

            // Kiểm tra xem user này có phải là customer của conv này không (bảo mật)
            var isParticipant = await _context.Conversations
                                            .AnyAsync(c => c.ConversationId == id && c.CustomerId == userId);
            
            if (!isParticipant) { return Forbid(); }

            var visibility = await _context.UserConversationVisibilities
                                        .FirstOrDefaultAsync(v => v.UserId == userId && v.ConversationId == id);

            if (visibility == null)
            {
                visibility = new UserConversationVisibility
                {
                    UserId = userId,
                    ConversationId = id,
                    IsHidden = true
                };

                _context.UserConversationVisibilities.Add(visibility);
            }
            else
            {
                visibility.IsHidden = true;
            }

            await _context.SaveChangesAsync();

            return Ok();
        
        }
    }
}
