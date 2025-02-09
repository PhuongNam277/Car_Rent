using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class TestimonialController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Pages";
            return View();
        }
    }
}
