using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Car_Rent.Models;
using Car_Rent.ViewModels.Car;

namespace Car_Rent.Controllers
{
    public class CarController : Controller
    {
        private readonly CarRentalDbContext _context;

        public CarController(CarRentalDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] CarFilterVM filters)
        {

            ViewData["ActivePage"] = "Pages";

            var q = _context.Cars.AsNoTracking()
                .Include(c => c.Category)
                .Where(c => c.Status == null || c.Status == "Available" || c.Status == "Rented");

            // Filters
            if (filters.CategoryId.HasValue)
                q = q.Where(c => c.CategoryId == filters.CategoryId.Value);

            if (filters.PriceMin.HasValue)
                q = q.Where(c => c.RentalPricePerDay >= filters.PriceMin.Value);

            if (filters.PriceMax.HasValue)
                q = q.Where(c => c.RentalPricePerDay <= filters.PriceMax.Value);

            if (filters.Seats.HasValue)
                q = q.Where(c => c.SeatNumber == filters.Seats.Value);

            if (!string.IsNullOrWhiteSpace(filters.Transmission))
                q = q.Where(c => c.TransmissionType == filters.Transmission);

            if(!string.IsNullOrWhiteSpace(filters.Energy))
                q = q.Where(c => c.EnergyType == filters.Energy);

            // Sort
            q = filters.SortBy switch
            {
                "PriceDesc" => q.OrderByDescending(c => c.RentalPricePerDay),
                "NameAsc" => q.OrderBy(c => c.CarName),
                "NameDesc" => q.OrderByDescending(c => c.CarName),
                "Newest" => q.OrderBy(c => c.SellDate).ThenByDescending(c => c.CarId),
                _        => q.OrderBy(c => c.RentalPricePerDay)
            };

            // Count before paging
            var total = await q.CountAsync();

            // Paging
            var skip = (Math.Max(1, filters.Page) - 1) * Math.Max(1, filters.PageSize);
            var items = await q
                .Skip(skip)
                .Take(filters.PageSize)
                .Select(c => new CarListItemVM
                {
                    CarId = c.CarId,
                    CarName = c.CarName,
                    Brand = c.Brand,
                    ImageUrl = c.ImageUrl,
                    RentalPricePerDay = c.RentalPricePerDay,
                    SeatNumber = c.SeatNumber,
                    EnergyType = c.EnergyType,
                    EngineType = c.EngineType,
                    TransmissionType = c.TransmissionType,
                    Status = c.Status ?? "Available",
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.CategoryName : ""
                })
                .ToListAsync();

            var cats = await _context.Categories.AsNoTracking()
                .Select(cat => new
                {
                    cat.CategoryId, cat.CategoryName,
                    Count = _context.Cars.Count(c => c.CategoryId == cat.CategoryId)
                })
                .ToListAsync();

            var vm = new CarIndexVM
            {
                Filters = filters,
                Cars = items,
                TotalItems = total,
                Categories = cats.Select(x => (x.CategoryId, x.CategoryName, x.Count)).ToList()
            };

            return View(vm);
        }

        // Quick view (partial)
        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var car = await _context.Cars.AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.BaseLocation)
                .FirstOrDefaultAsync(c => c.CarId == id);

            if (car == null) { return NotFound(); }

            var vm = new CarListItemVM
            {
                CarId = car.CarId,
                CarName = car.CarName,
                Brand = car.Brand,
                ImageUrl = car.ImageUrl,
                RentalPricePerDay = car.RentalPricePerDay,
                SeatNumber = car.SeatNumber,
                EnergyType = car.EnergyType,
                EngineType = car.EngineType,
                TransmissionType = car.TransmissionType,
                Status = car.Status ?? "Available",
                CategoryId = car.CategoryId,
                CategoryName = car.Category?.CategoryName ?? ""
            };

            return PartialView("_CarQuickView", vm);
        }

        // Compare (partial) - nhận tối đa 3 id
        [HttpGet]
        public async Task<IActionResult> ComparePartial([FromQuery] int[] ids)
        {
            ids = ids?.Distinct().Take(3).ToArray() ?? Array.Empty<int>();
            var cars = await _context.Cars.AsNoTracking()
                .Include(c => c.Category)
                .Where(c => ids.Contains(c.CarId))
                .Select(c => new CarListItemVM
                {
                    CarId = c.CarId,
                    CarName = c.CarName,
                    Brand = c.Brand,
                    ImageUrl = c.ImageUrl,
                    RentalPricePerDay = c.RentalPricePerDay,
                    SeatNumber = c.SeatNumber,
                    EnergyType = c.EnergyType,
                    EngineType = c.EngineType,
                    TransmissionType = c.TransmissionType,
                    Status = c.Status ?? "Available",
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.CategoryName : ""
                })
                .ToListAsync();

            return PartialView("_CarCompare", cars);
        }

        // GET: Car
        public async Task<IActionResult> AdminIndex(string? search, string? sortBy = "NameAsc", int page = 1, int pageSize = 10)
        {
            // Ghi nhớ tham số tìm kiếm hiện tại
            ViewData["CurrentSearch"] = search;

            var query = _context.Cars
                .Include(c => c.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.CarName.Contains(search) || c.Brand.Contains(search) || c.Model.Contains(search) || c.RentalPricePerDay.ToString().Contains(search));
            }

            // Sorting logic
            query = sortBy switch
            {
                "NameDesc" => query.OrderByDescending(c => c.CarName),
                "RentalPriceAsc" => query.OrderBy(c => c.RentalPricePerDay),
                "RentalPriceDesc" => query.OrderByDescending(c => c.RentalPricePerDay),
                _ => query.OrderBy(c => c.CarName)
            };

            // Pagination logic
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.SortBy = sortBy;
            ViewBag.TotalItems = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            return View(items);
        }

        // GET: Car/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.CarId == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // GET: Car/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName");
            return View();
        }

        // POST: Car/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                // Upload ảnh
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/cars");

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(stream);
                    }

                    car.ImageUrl = "/images/cars/" + fileName;
                }

                _context.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(AdminIndex));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", car.CategoryId);
            return View(car);
        }

        // GET: Car/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars.FindAsync(id);
            if (car == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryName", car.CategoryId);
            return View(car);
        }

        // POST: Car/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Car car, IFormFile? ImageFile)
        {
            if (id != car.CarId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy bản ghi cũ để giữ nguyên ảnh nếu không upload mới
                    var existingCar = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.CarId == id);

                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/cars");

                        if (!Directory.Exists(uploadPath))
                            Directory.CreateDirectory(uploadPath);

                        var filePath = Path.Combine(uploadPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        car.ImageUrl = "/images/cars/" + fileName;
                    }
                    else
                    {
                        // Không chọn ảnh mới => giữ nguyên ảnh cũ
                        car.ImageUrl = existingCar?.ImageUrl;
                    }

                    _context.Update(car);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CarExists(car.CarId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(AdminIndex));
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "CategoryId", "CategoryId", car.CategoryId);
            return View(car);
        }

        // GET: Car/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var car = await _context.Cars
                .Include(c => c.Category)
                .FirstOrDefaultAsync(m => m.CarId == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // POST: Car/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                _context.Cars.Remove(car);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminIndex));
        }

        private bool CarExists(int id)
        {
            return _context.Cars.Any(e => e.CarId == id);
        }
    }
}
