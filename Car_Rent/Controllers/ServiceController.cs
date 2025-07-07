using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    public class ServiceController : Controller
    {

        private readonly CarRentalDbContext _context;

        public ServiceController(CarRentalDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            // 1. Lay ds reviews
            // 

            var reviewEntities = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Car)
                .ToList();

            // 2. Khoi tao ServiceViewModel
            var serviceViewModel = new ServiceViewModel
            {
                Reviews = reviewEntities
            };


            ViewData["ActivePage"] = "Service";
            return View(serviceViewModel);
        }
    }
}
