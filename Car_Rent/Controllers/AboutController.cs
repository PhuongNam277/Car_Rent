using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "About";
            return View();
        }
    }
}
