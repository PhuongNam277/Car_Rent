using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;

namespace Car_Rent.Controllers
{
    public class CategoryController : Controller
    {
        private readonly CarRentalDbContext _context;

        public CategoryController(CarRentalDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View();
        }

    }
}
