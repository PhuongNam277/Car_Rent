using Car_Rent.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class ContactController : Controller
    {
        private readonly CarRentalDbContext _context;

        public ContactController(CarRentalDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Contact";
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Send(Contact model)
        {
            if (ModelState.IsValid)
            {
                model.SubmittedDate = DateTime.Now;
                model.Status = "New"; // Mặc định trạng thái là "New"

                _context.Contacts.Add(model);
                await _context.SaveChangesAsync();

                // Gửi thành công → tạo thông báo
                TempData["SuccessMessage"] = "Your message has been sent successfully!";

                return RedirectToAction("Index");
            }
            // Gửi thành công → tạo thông báo
            TempData["FailedMessage"] = "Your message failed to send !";
            return View("Index", model);
        }
    }
}
