using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class BookcarController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
