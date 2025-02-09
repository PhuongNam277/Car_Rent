using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class TeamController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Pages";
            return View();
        }
    }
}
