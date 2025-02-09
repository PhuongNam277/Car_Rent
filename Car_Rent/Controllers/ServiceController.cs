using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class ServiceController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Service";
            return View();
        }
    }
}
