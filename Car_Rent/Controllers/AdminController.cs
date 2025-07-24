using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class AdminController : Controller
    {
        private readonly CarRentalDbContext _context;

        public AdminController(CarRentalDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Role()
        {
            return View("Role", _context.Roles.ToList());
        }









    }
}
