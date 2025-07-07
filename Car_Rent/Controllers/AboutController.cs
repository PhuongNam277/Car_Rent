using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class AboutController : Controller
    {

        private readonly CarRentalDbContext _context;

        public AboutController(CarRentalDbContext context)
        {
            _context = context;
        }

        // GET: About/Index
        public IActionResult Index()
        {
            // 1. Lay ds customer
            var userEntities = _context.Users.ToList();

            var aboutViewModel = new AboutViewModel
            {
                Users = userEntities
            };





            ViewData["ActivePage"] = "About";
            return View(aboutViewModel);
        }
    }
}
