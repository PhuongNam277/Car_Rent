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

        // Check login & create reservation in BookNow method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookNow(ReserveRequest req, string? returnUrl = null)
        {
            // if not logged in, redirect to login page 
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                var backUrl = Url.Action("Index", "Bookcar", new { carId = req.CarId, categoryId = req.CategoryId});
                return RedirectToAction("Index", "Login", new { returnUrl = backUrl });

            }

            var userId = GetUserIdFromClaims(User);
            if(userId == null)
            {
                // User not found, redirect to login
                await HttpContext.SignOutAsync("MyCookieAuth");
                return RedirectToAction("Index", "Login");
            }

            // Connect date and time
            var startDateTime = CombineDateTime(req.StartDate, req.StartTime);
            var endDateTime = CombineDateTime(req.EndDate, req.EndTime);
            if(startDateTime >= endDateTime)
            {
                ModelState.AddModelError("", "Start date and time must be before end date and time.");
                return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
            }

            // Check if the car is available
            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == req.CarId);
            if (car == null)
            {
                ModelState.AddModelError("", "Car not found.");
                return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
            }

            var reservation = new Reservation
            {
                UserId = userId.Value,
                CarId = req.CarId.Value,
                FromCity = req.FromCity,
                ToCity = req.ToCity,
                StartDate = startDateTime,
                EndDate = endDateTime,
                Status = "Pending",
                ReservationDate = DateTime.Now
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Redirect to payment page
            return RedirectToAction("ConfirmPayment", "Payment", new { reservationId = reservation.ReservationId});
        }


        // Combine datetime method
        private static DateTime CombineDateTime(DateTime date, string timeHHMM)
        {
            if (!TimeSpan.TryParse(timeHHMM, out var ts)) ts = TimeSpan.Zero;
            return date.Date.Add(ts);
        }




    }
}
