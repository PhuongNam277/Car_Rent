using System.Security.Claims;
using Car_Rent.DTOs;
using Car_Rent.Models;
using Car_Rent.ViewModels.Car;
using Microsoft.AspNetCore.Authentication;
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

        //public async Task<IActionResult> Index(int? carId, int? categoryId)
        //{
        //    var viewModel = new CategoryCarModel
        //    {
        //        Categories = _context.Categories.ToList(),
        //        Cars = categoryId != null
        //        ? _context.Cars.Where(c => c.CategoryId == categoryId).ToList()
        //        : _context.Cars.ToList()
        //    };

        //    ViewBag.SelectedCarId = carId;
        //    ViewBag.SelectedCategoryId = categoryId;

        //    // New - Load locations for dropdowns
        //    ViewBag.Locations = await _context.Locations
        //                                .Where(l => l.IsActive)
        //                                .OrderBy(l => l.Name)
        //                                .ToListAsync();
        //    return View(viewModel);
        //}

        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId = null, int? carId = null)
        {
            var categories = await _context.Categories
                .IgnoreQueryFilters()
                .Where(c => c.IsActive == true)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();

            var carsQuery = _context.Cars
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.Status == "Available");

            if (categoryId.HasValue)
                carsQuery = carsQuery.Where(c => c.CategoryId == categoryId.Value);

            var cars = await carsQuery
                .OrderBy(c => c.CarName)
                .Take(100)
                .Select(c => new CarOptionVM
                {
                    CarId = c.CarId,
                    CarName = c.CarName,
                    LicensePlate = c.LicensePlate
                })
                .ToListAsync();

            var locations = await _context.Locations
                .IgnoreQueryFilters()
                .Include(l => l.Tenant)
                .AsNoTracking()
                .Where(l => l.IsActive == true)
                .Select(l => new LocationOptionVM
                {
                    LocationId = l.LocationId,
                    Name = l.Name,
                    TenantId = l.TenantId,
                    TenantName = l.Tenant!.Name
                })
                .OrderBy(x => x.TenantName).ThenBy(x => x.Name)
                .ToListAsync();

            var model = new BookcarViewModel
            {
                Categories = categories,
                Cars = cars,
                SelectedCategoryId = categoryId,
                SelectedCarId = carId
            };

            ViewBag.Locations = locations;

            return View(model);
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

        // New: Search - car available in a station + time range
        //[HttpGet]
        //public async Task<IActionResult> AvailableCars([FromQuery] SearchAvailabilityRequest req, [FromQuery] int? tenantId)
        //{
        //    var start = CombineDateTime(req.StartDate, req.StartTime);
        //    var end = CombineDateTime(req.EndDate, req.EndTime);
        //    if(end <= start)
        //    {
        //        return BadRequest("End date and time must be after start date and time.");
        //    }

        //    // Update 25_09_2025
        //    // Base query theo tenant (marketplace có thể khác tenant hiện hành)
        //    IQueryable<Car> carsQ = _context.Cars.AsNoTracking();

        //    if (tenantId.HasValue)
        //        carsQ = carsQ.IgnoreQueryFilters().Where(c => c.TenantId == tenantId.Value);

        //    // lọc theo station pick-up (bắt buộc)
        //    carsQ = carsQ.Where(c => c.BaseLocationId == req.PickupLocationId);

        //    // lọc theo category nếu có
        //    if (req.CategoryId.HasValue)
        //        carsQ = carsQ.Where(c => c.CategoryId == req.CategoryId.Value);

        //    // chặn trùng lịch + buffer 1h
        //    var buf = TimeSpan.FromHours(1);
        //    var startWithBuf = start - buf;
        //    var endWithBuf = end + buf;

        //    var cars = await carsQ
        //        .Where(c => !_context.Reservations
        //            .IgnoreQueryFilters()
        //            .Any(r =>
        //                r.TenantId == c.TenantId &&
        //                r.CarId == c.CarId &&
        //                r.Status != "Cancelled" &&
        //                r.StartDate < endWithBuf && startWithBuf < r.EndDate

        //            )
        //        )
        //        .Select(c => new { c.CarId, c.CarName })
        //        .ToListAsync();

        //    return Json(cars);
        //}

        [HttpGet]
        public async Task<IActionResult> AvailableCars(
    int pickupLocationId,
    DateTime startDate, string startTime,
    DateTime endDate, string endTime,
    int? categoryId,
    int? tenantId
)
        {
            static string HHmm(string? v) => !string.IsNullOrWhiteSpace(v) && v!.Length == 5 ? v! : "12:00";
            if (!TimeSpan.TryParse(HHmm(startTime), out var startTs) ||
                !TimeSpan.TryParse(HHmm(endTime), out var endTs))
                return BadRequest(new { message = "Invalid time format. Expect HH:mm." });

            var start = startDate.Date + startTs;
            var end = endDate.Date + endTs;
            if (end <= start)
                return BadRequest(new { message = "End time must be after start time." });

            // Suy ra tenantId nếu chưa có
            if (tenantId is null)
            {
                tenantId = await _context.Locations
                    .IgnoreQueryFilters()
                    .Where(l => l.LocationId == pickupLocationId && l.IsActive == true)
                    .Select(l => (int?)l.TenantId)
                    .FirstOrDefaultAsync();

                if (tenantId is null)
                    return BadRequest(new { message = "Cannot infer tenant from pickup station." });
            }

            var pickupOk = await _context.Locations
                .IgnoreQueryFilters()
                .AnyAsync(l => l.LocationId == pickupLocationId
                            && l.TenantId == tenantId.Value
                            && l.IsActive == true);
            if (!pickupOk)
                return BadRequest(new { message = "Invalid pickup station for this tenant." });

            var busyStatuses = new[] { "Pending", "Confirmed", "Active" };

            var carsQuery = _context.Cars
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(c => c.TenantId == tenantId.Value
                         && c.Status == "Available"
                         && c.BaseLocationId == pickupLocationId);

            if (categoryId.HasValue)
                carsQuery = carsQuery.Where(c => c.CategoryId == categoryId.Value);

            var available = await carsQuery
                .Where(c => !_context.Reservations
                    .IgnoreQueryFilters()
                    .Any(r => r.CarId == c.CarId
                           && r.TenantId == tenantId.Value
                           && busyStatuses.Contains(r.Status)
                           && r.StartDate < end
                           && r.EndDate > start))
                .OrderBy(c => c.CarName)
                .Select(c => new
                {
                    carId = c.CarId,
                    carName = c.CarName,
                    imageUrl = c.ImageUrl,
                    categoryId = c.CategoryId
                })
                .ToListAsync();

            return Json(available);
        }




        // Get UserId from Claims
        private int? GetUserIdFromClaims(ClaimsPrincipal user)
        {
            var claim = user.FindFirst("UserId");
            if (claim == null)
            {
                return null;
            }

            if (int.TryParse(claim.Value, out var id)) return id;

            return null;
        }

        // NEW: Reserve - BookNow method using ReserveRequest DTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookNow (ReserveRequest req)
        {
            // if not logged in, redirect to login page
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                var backUrl = Url.Action("Index", "Bookcar", new { carId = req.CarId, categoryId = req.CategoryId });
                return RedirectToAction("Index", "Login", new { returnUrl = backUrl });
            }

            var userId = GetUserIdFromClaims(User);
            if(userId == null)
            {
                // User not found, redirect to login
                await HttpContext.SignOutAsync("MyCookieAuth");
                return RedirectToAction("Index", "Login");
            }

            // Combine date and time

            var startDateTime = CombineDateTime(req.StartDate, req.StartTime);
            var endDateTime = CombineDateTime(req.EndDate, req.EndTime);
            if (startDateTime >= endDateTime)
            {
                ModelState.AddModelError("", "Start date and time must be before end date and time.");
                return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
            }

            // MVP check if the car is available
            if (req.DropoffLocationId == 0) req.DropoffLocationId = req.PickupLocationId;

            // check car
            //var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == req.CarId);
            //if (car == null)
            //{
            //    ModelState.AddModelError("", "Car not found.");
            //    return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
            //}

            var car = await _context.Cars
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CarId == req.CarId);

            if (car == null)
            {
                ModelState.AddModelError("", "Car not found.");
                return RedirectToAction("Index", new {carId = req.CarId, categoryId = req.CategoryId});
            }

            req.TenantId = car.TenantId;

            // Bat buoc xe thuoc dung station pick-up
            if (car.BaseLocationId != req.PickupLocationId)
            {
                ModelState.AddModelError("", "Selected car is not at the pick up station");
                return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
            }

            // Giu cho an toan bang transaction (tranh race)
            using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            try
            {
                // Double-check trong transaction
                bool busyNow = await _context.Reservations.AnyAsync(r =>
                    r.CarId == car.CarId &&
                    r.Status != "Cancelled" &&
                    r.StartDate < endDateTime && startDateTime < r.EndDate
                );

                if(busyNow)
                {
                    await tx.RollbackAsync();
                    ModelState.AddModelError("", "Car just got booked. Please choose another car.");
                    return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
                
                }

                decimal total = CalculateTotalPrice(startDateTime, endDateTime, car.RentalPricePerDay);

                // Luu du lieu qua TempData
                req.TotalPrice = total;
                req.StartDate = startDateTime;
                req.EndDate = endDateTime;

                TempData["ConfirmReservation"] = System.Text.Json.JsonSerializer.Serialize(req);

                await tx.CommitAsync();
                return RedirectToAction("ConfirmPayment", "Payment");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }



        //Combine datetime method
        private static DateTime CombineDateTime(DateTime date, string timeHHMM)
        {
            if (!TimeSpan.TryParse(timeHHMM, out var ts)) ts = TimeSpan.Zero;
            return date.Date.Add(ts);
        }

        //Calculate total price method
        private decimal CalculateTotalPrice(DateTime startDate, DateTime endDate, decimal pricePerDay)
        {
            var totalDays = (endDate - startDate).TotalDays;
            if (totalDays < 0) return 0; // Invalid date range
            return Math.Round((decimal)totalDays * pricePerDay, 2);
        }




    }
}
