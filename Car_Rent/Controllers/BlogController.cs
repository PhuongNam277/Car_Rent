using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class BlogController : Controller
    {
        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Blog";
            return View();
        }
    }
}
