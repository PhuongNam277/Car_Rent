using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class _404Controller : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Pages";
            return View();
        }
    }
}
