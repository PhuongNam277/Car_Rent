using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    public class BookcarController : Controller
    {
        private readonly CarRentalDbContext _context;

        public BookcarController(CarRentalDbContext context)
        {
            _context = context;
        } 

        public async Task<IActionResult> Index()
        {
            var viewModel = new CategoryCarModel
            {
                Categories = await _context.Categories.ToListAsync(),
                Cars = await _context.Cars.ToListAsync()
            };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetCarsByCategory(int categoryId)
        {
            var cars = await _context.Cars
                .Where(c => c.CategoryId == categoryId)
                .Select(c => new { c.CarId, c.CarName })
                .ToListAsync();

            return Json(cars);
        }

    }
}
