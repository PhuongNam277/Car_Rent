using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
