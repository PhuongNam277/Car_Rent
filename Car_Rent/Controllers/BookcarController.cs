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

        public async Task<IActionResult> Index(int? carId, int? categoryId)
        {
            var viewModel = new CategoryCarModel
            {
                Categories = _context.Categories.ToList(),
                Cars = categoryId != null
            ? _context.Cars.Where(c => c.CategoryId == categoryId).ToList()
            : _context.Cars.ToList()
            };

            ViewBag.SelectedCarId = carId;
            ViewBag.SelectedCategoryId = categoryId;
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
