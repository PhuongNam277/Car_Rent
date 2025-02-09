using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class FeatureController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Pages";
            return View();
        }
    }
}
