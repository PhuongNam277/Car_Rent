using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class CarController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Pages";
            return View();
        }
    }
}
