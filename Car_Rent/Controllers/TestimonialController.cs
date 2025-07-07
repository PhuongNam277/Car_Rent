using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    public class TestimonialController : Controller
    {
        private readonly CarRentalDbContext _context;

        public TestimonialController(CarRentalDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> Index()
        {
            ViewData["ActivePage"] = "Pages";

            // Lay danh sach review tu db
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Car)
                .ToListAsync();

            var testimonialViewModel = new TestimonialViewModel
            {
                Reviews = reviews
            };

            return View(testimonialViewModel);
        }
    }
}
