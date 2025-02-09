using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class MainController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Home";
            return View();
        }
    }
}
