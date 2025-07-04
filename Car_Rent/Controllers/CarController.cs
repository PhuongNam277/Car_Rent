using Car_Rent.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Car_Rent.Controllers
{
    public class CarController : Controller
    {
        private readonly CarRentalDbContext _context;

        public CarController(CarRentalDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["ActivePage"] = "Pages";

            

            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewData["ActivePage"] = "Pages";
            var categories = _context.Categories
                .Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                }).ToList();

            ViewBag.CategoryId = categories;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car)
        {
            if (!ModelState.IsValid)
            {
                // Gõ lệnh này trong cửa sổ Watch hoặc Immediate
                var allErrors = ModelState.Values
                                           .SelectMany(v => v.Errors)
                                           .Select(e => e.ErrorMessage)
                                           .ToList();        // đặt breakpoint ở đây

                // Hoặc tạm ghi log:
                //_logger.LogWarning(string.Join("; ", allErrors));

                // NHỚ nạp lại ViewBag.CategoryId trước khi return View
                LoadCategories();            // hàm tự viết
                return View(car);
            }

            if (ModelState.IsValid)
            {
                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            ViewData["ActivePage"] = "Pages";
            return View(car);
        }

        private void LoadCategories()
        {
            ViewBag.CategoryId = _context.Categories
            .Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.CategoryName
            }).ToList();
        }
    }
}
