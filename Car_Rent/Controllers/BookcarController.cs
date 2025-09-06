using System.Security.Claims;
using Car_Rent.DTOs;
using Car_Rent.Models;
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

            // New - Load locations for dropdowns
            ViewBag.Locations = await _context.Locations
                                        .Where(l => l.IsActive)
                                        .OrderBy(l => l.Name)
                                        .ToListAsync();
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

        // New: Search - car available in a station + time range
        [HttpGet]
        public async Task<IActionResult> AvailableCars([FromQuery] SearchAvailabilityRequest req)
        {
            var start = CombineDateTime(req.StartDate, req.StartTime);
            var end = CombineDateTime(req.EndDate, req.EndTime);
            if(end <= start)
            {
                return BadRequest("End date and time must be after start date and time.");
            }

            var query = _context.Cars
                .Where(c => c.BaseLocationId == req.PickupLocationId);

            if (req.CategoryId.HasValue) query = query.Where(c => c.CategoryId == req.CategoryId.Value);

            var cars = await query
                .Where(c => !_context.Reservations.Any(r =>
                    r.CarId == c.CarId &&
                    r.Status != "Cancelled" &&                  // chỉ loại đơn chưa hủy
                    r.StartDate < end && start < r.EndDate))    // điều kiện overlap chuẩn
                .Select(c => new { c.CarId, c.CarName })
                .ToListAsync();

            return Json(cars);
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
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == req.CarId);
            if (car == null)
            {
                ModelState.AddModelError("", "Car not found.");
                return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
            }

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

        //// Check login & create reservation in BookNow method
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> BookNow(ReserveRequest req, string? returnUrl = null)
        //{
        //    // if not logged in, redirect to login page 
        //    if (!User.Identity?.IsAuthenticated ?? true)
        //    {
        //        var backUrl = Url.Action("Index", "Bookcar", new { carId = req.CarId, categoryId = req.CategoryId});
        //        return RedirectToAction("Index", "Login", new { returnUrl = backUrl });

        //    }

        //    var userId = GetUserIdFromClaims(User);
        //    if(userId == null)
        //    {
        //        // User not found, redirect to login
        //        await HttpContext.SignOutAsync("MyCookieAuth");
        //        return RedirectToAction("Index", "Login");
        //    }

        //    // Connect date and time
        //    var startDateTime = CombineDateTime(req.StartDate, req.StartTime);
        //    var endDateTime = CombineDateTime(req.EndDate, req.EndTime);
        //    if (startDateTime >= endDateTime)
        //    {
        //        ModelState.AddModelError("", "Start date and time must be before end date and time.");
        //        return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
        //    }

        //    // Check if the car is available
        //    var car = await _context.Cars
        //        .FirstOrDefaultAsync(c => c.CarId == req.CarId);
        //    if (car == null)
        //    {
        //        ModelState.AddModelError("", "Car not found.");
        //        return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
        //    }

        //    //var reservation = new Reservation
        //    //{
        //    //    UserId = userId.Value,
        //    //    CarId = req.CarId,
        //    //    FromCity = req.FromCity,
        //    //    ToCity = req.ToCity,
        //    //    StartDate = startDateTime,
        //    //    EndDate = endDateTime,
        //    //    Status = "Pending",
        //    //    TotalPrice = CalculateTotalPrice(startDateTime, endDateTime, car.RentalPricePerDay),
        //    //    ReservationDate = DateTime.Now
        //    //};

        //    //_context.Reservations.Add(reservation);
        //    //await _context.SaveChangesAsync();

        //    //// Redirect to payment page
        //    //return RedirectToAction("ConfirmPayment", "Payment", new { reservationId = reservation.ReservationId});

        //    // Send to Confirmation page with reservation details
        //    //var totalPrice = CalculateTotalPrice(startDateTime, endDateTime, car.RentalPricePerDay);

        //    decimal total = CalculateTotalPrice(startDateTime, endDateTime, car.RentalPricePerDay);
        //    req.StartDate = startDateTime;
        //    req.EndDate = endDateTime;
        //    req.TotalPrice = total; // Format as currency

        //    // Use TempData to pass data to the next request
        //    TempData["ConfirmReservation"] = System.Text.Json.JsonSerializer.Serialize(req);
        //    return RedirectToAction("ConfirmPayment", "Payment");

        //}



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
