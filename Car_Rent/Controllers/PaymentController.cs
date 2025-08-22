using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Car_Rent.Models;
using Car_Rent.DTOs;
using Microsoft.AspNetCore.Authentication;

namespace Car_Rent.Controllers
{
    public class PaymentController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly EmailService _emailService;


        public PaymentController(CarRentalDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Payment ( For Admin Page)
        public async Task<IActionResult> Index(string search, string? sortBy = "PaymentDateDesc", int page = 1, int pageSize = 10)
        {

            var query = _context.Payments
                .Include(p => p.Reservation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                p.PaymentMethod.Contains(search) ||
                p.Status.Contains(search) ||
                p.Reservation.FromCity.Contains(search) ||
                p.Reservation.ToCity.Contains(search));
            }

            // Sorting logic
            query = sortBy switch
            {
                "PaymentDateAsc" => query.OrderBy(p => p.PaymentDate),
                "AmountAsc" => query.OrderBy(p => p.Amount),
                "AmountDesc" => query.OrderByDescending(p => p.Amount),
                _ => query.OrderByDescending(p => p.PaymentDate)
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

        // GET: Payment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Reservation)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payment/Create
        public IActionResult Create()
        {
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "ReservationId", "ReservationId");
            return View();
        }

        // POST: Payment/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PaymentId,ReservationId,PaymentDate,Amount,PaymentMethod,Status")] Payment payment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "ReservationId", "ReservationId", payment.ReservationId);
            return View(payment);
        }

        // GET: Payment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "ReservationId", "ReservationId", payment.ReservationId);
            return View(payment);
        }

        // POST: Payment/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PaymentId,ReservationId,PaymentDate,Amount,PaymentMethod,Status")] Payment payment)
        {
            if (id != payment.PaymentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.PaymentId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["ReservationId"] = new SelectList(_context.Reservations, "ReservationId", "ReservationId", payment.ReservationId);
            return View(payment);
        }

        // GET: Payment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Reservation)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }

        // ConfirmPayment method

        private int? GetUserId()
        {
            var claim = User.FindFirst("UserId");
            if (claim == null)
            {
                return null;
            }
            return int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }

        // Get : ConfirmPayment
        [HttpGet]
        public async Task<IActionResult> ConfirmPayment()
        {
            // Get data from TempData by BookNow set
            var json = TempData["ConfirmReservation"] as string;

            // Use Fresh until submit
            if (json != null) TempData.Keep("ConfirmReservation");

            if (string.IsNullOrEmpty(json))
            {
                TempData["FailedMessage"] = "No reservation data found. Please try again.";
                return RedirectToAction("Index", "Bookcar");
            }

            var req = System.Text.Json.JsonSerializer.Deserialize<ReserveRequest>(json);

            if (req == null)
            {
                TempData["FailedMessage"] = "Invalid reservation data. Please try again.";
                return RedirectToAction("Index", "Bookcar");
            }

            var car = await _context.Cars
                .FirstOrDefaultAsync(c => c.CarId == req.CarId);

            ViewBag.CarName = car?.CarName ?? $"Car #{req.CarId}"; // Default car name if not found

            return View(req);
        }

        // POST: /Payment/PayCash => Save to DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayCash()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Index", "Login", new { returnUrl = Url.Action("ConfirmPayment", "Payment") });
            }

            var userId = GetUserId();

            if (userId == null)
            {
                TempData["FailedMessage"] = "User not found. Please log in again.";
                await HttpContext.SignOutAsync("MyCookieAuth");
                return RedirectToAction("Index", "Login");
            }

            

            var json = TempData["ConfirmReservation"] as string;
            if (string.IsNullOrEmpty(json))
            {
                TempData["FailedMessage"] = "No reservation data found. Please try again.";
                return RedirectToAction("Index", "Bookcar");
            }

            var req = System.Text.Json.JsonSerializer.Deserialize<ReserveRequest>(json);
            Console.WriteLine(req); // Debugging line
            if (req == null)
            {
                TempData["FailedMessage"] = "Invalid reservation data. Please try again.";
                return RedirectToAction("Index", "Bookcar");
            }

            // Check car
            //var car = await _context.Cars
            //    .Where(c => c.CarId == req.CarId)
            //    .Select(c => new { c.RentalPricePerDay })
            //    .FirstOrDefaultAsync();

            //if (car == null)
            //{
            //    TempData["FailedMessage"] = "Car not found. Please try again.";
            //    return RedirectToAction("Index", "Bookcar");
            //}

            // Save to DB
            var startDateTime = CombineDateTime(req.StartDate, req.StartTime);
            var endDateTime = CombineDateTime(req.EndDate, req.EndTime);

            if (startDateTime >= endDateTime)
            {
                ModelState.AddModelError("", "Start date and time must be before end date and time.");
                return RedirectToAction("Index", new { carId = req.CarId, categoryId = req.CategoryId });
            }

            //var total = CalculateTotalPrice(startDateTime, endDateTime, car.RentalPricePerDay);
            //Console.WriteLine($"Total Price: {total}"); // Debugging line
            //ViewBag.TotalPrice = CalculateTotalPrice(req.StartDate, req.EndDate, car?.RentalPricePerDay ?? 0m);

            // Create a reservation object
            var reservation = new Reservation
            {
                UserId = userId.Value,
                CarId = req.CarId,
                ReservationDate = DateTime.Now,
                StartDate = startDateTime,
                EndDate = endDateTime,
                FromCity = req.FromCity,
                ToCity = req.ToCity,
                Status = "Pending",
                TotalPrice = req.TotalPrice

            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            // Save to payment
            var payment = new Payment
            {
                ReservationId = reservation.ReservationId,
                PaymentDate = DateTime.Now,
                Amount = reservation.TotalPrice,
                PaymentMethod = "Cash",
                Status = "Pending"
            };

            // Set car to unavailable
            var car = await _context.Cars.FindAsync(req.CarId);
            if (car == null)
            {
                TempData["FailedMessage"] = "Car not found. Please try again.";
                return RedirectToAction("Index", "Bookcar");
            }

            car.Status = "Rented";
            _context.Cars.Update(car);

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Clean TempData
            TempData.Remove("ConfirmReservation");

            // Display success message
            return RedirectToAction(nameof(Success), new { reservationId = reservation.ReservationId });
        }

        // Combine datetime method
        private static DateTime CombineDateTime(DateTime date, string timeHHMM)
        {
            if (!TimeSpan.TryParse(timeHHMM, out var ts)) ts = TimeSpan.Zero;
            return date.Date.Add(ts);
        }

        // Calculate total price method
        private decimal CalculateTotalPrice(DateTime startDate, DateTime endDate, decimal pricePerDay)
        {
            var totalDays = (endDate - startDate).TotalDays;
            if (totalDays <= 0) return 0;
            var billableDays = Math.Max(1, (int)Math.Ceiling(totalDays)); // tối thiểu 1 ngày
            return Math.Round(billableDays * pricePerDay, 2);
        }

        // GET: /Payment/Success
        [HttpGet]
        public async Task<IActionResult> Success(int reservationId)
        {
            // Get user email
            var userId = GetUserId();

            if (userId == null)
            {
                TempData["FailedMessage"] = "User not found. Please log in again.";
                await HttpContext.SignOutAsync("MyCookieAuth");
                return RedirectToAction("Index", "Login");
            }

            var user = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => new { u.Email })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                TempData["FailedMessage"] = "User not found. Please log in again.";
                await HttpContext.SignOutAsync("MyCookieAuth");
                return RedirectToAction("Index", "Login");
            }

            // Send email confirmation
            var emailBody = $"Your reservation with ID {reservationId} has been successfully created. " +
                            $"You can view your reservation details in your account.";
            await _emailService.SendEmailAsync(user.Email, "Reservation Confirmation", emailBody);




            //var res = await _context.Reservations
            //    .Include(r => r.Car)
            //    .FirstOrDefaultAsync(r => r.ReservationId == reservationId);


            //if (res == null) return RedirectToAction("Index", "Bookcar");
            //return View(res);

            TempData["Success"]= "Your reservation has been successfully created. " +
                                 "A confirmation email has been sent to your registered email address." +
                                 "This is your reservation detail";

            return RedirectToAction("Details", "MyReservation", new { id = reservationId });
        }

        // POST: /Payment/PayMomo (developing)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayMomo()
        {
            // Implement Momo payment logic here
            // This is a placeholder for the actual implementation
            TempData["FailedMessage"] = "Momo payment is not implemented yet.";
            TempData.Keep("ConfirmReservation");
            return RedirectToAction("ConfirmPayment", "Payment");

        }
    }
}
