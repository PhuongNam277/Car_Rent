using System.Security.Claims;
using Car_Rent.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth")]
    public class ChatController : Controller
    {
        private readonly IChatService _chat;
        public ChatController(IChatService chat)
        {
            _chat = chat;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Khi bam "Chat Now" => Tao hoac lay conversation dang Open
        [HttpGet]
        public async Task<IActionResult> Start()
        {
            var customerId = GetUserId();
            var conv = await _chat.GetOrCreateConversationForCustomerAsync(customerId);
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

        public IActionResult Index()
        {
            return View();
        }
    }
}
