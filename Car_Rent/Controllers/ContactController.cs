using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Contact";
            return View();
        }
    }
}
