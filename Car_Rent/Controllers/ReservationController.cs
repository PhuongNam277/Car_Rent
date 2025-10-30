using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Car_Rent.Models;
using Car_Rent.Application.Common;

namespace Car_Rent.Controllers
{
    public class ReservationController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly ILogger<ReservationController> _logger;

        public ReservationController(CarRentalDbContext context, ILogger<ReservationController> logger)
        {
            _context = context;
            _logger = logger;
        }



        // GET: Reservation
        // Admin Index
        public async Task<IActionResult> Index(string? search, string? status, string? sortBy = "DateDesc", int page = 1, int pageSize = 10) 
        {
            //var carRentalDbContext = _context.Reservations.Include(r => r.Car).Include(r => r.User);
            //return View(await carRentalDbContext.ToListAsync());

            // Ghi nhớ tham số tìm kiếm hiện tại
            ViewData["CurrentSearch"] = search;

            var query = _context.Reservations
                .Include(r => r.Car)
                .Include(r => r.User)
                .Include(r => r.PickupLocation)
                .Include(r => r.DropoffLocation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.User.Username.Contains(search) || r.Car.CarName.Contains(search)
                                     || r.TotalPrice.ToString().Contains(search) || r.Status.Contains(search));
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(r => r.Status == status);
            }

            // Sorting logic
            query = sortBy switch
            {
                "DateAsc" => query.OrderBy(r => r.ReservationDate),
                "PriceAsc" => query.OrderBy(r => r.TotalPrice),
                "PriceDesc" => query.OrderByDescending(r => r.TotalPrice),
                "StartDateAsc" => query.OrderBy(r => r.StartDate),
                 _ => query.OrderByDescending(r => r.ReservationDate) // Default sort by ReservationDate descending
            };

            // Pagination logic
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.SortBy = sortBy;
            ViewBag.Status = string.IsNullOrWhiteSpace(status) ? "All" : status;
            ViewBag.TotalItems = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            return View(items);
        }

        // GET: Reservation/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Car)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReservationId == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // GET: Reservation/Create
        public IActionResult Create()
        {
            ViewData["CarId"] = new SelectList(_context.Cars, "CarId", "CarName");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username");
            return View();
        }

        // POST: Reservation/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ReservationId,UserId,CarId,ReservationDate,StartDate,EndDate,TotalPrice,FromCity, ToCity, Status, PickupLocationId, DropoffLocationId")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                // Map TenantId theo Car
                var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == reservation.CarId);
                if(car != null) reservation.TenantId = car.TenantId;

                _context.Add(reservation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["CarId"] = new SelectList(_context.Cars, "CarId", "CarName", reservation.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username", reservation.UserId);
            return View(reservation);
        }

        // GET: Reservation/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation == null)
            {
                return NotFound();
            }
            ViewData["CarId"] = new SelectList(_context.Cars, "CarId", "CarName", reservation.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username", reservation.UserId);

            // NEW:
            ViewData["PickupLocationId"] = new SelectList(_context.Locations.Where(l => l.IsActive), "LocationId", "Name", reservation.PickupLocationId);
            ViewData["DropoffLocationId"] = new SelectList(_context.Locations.Where(l => l.IsActive), "LocationId", "Name", reservation.DropoffLocationId); 
            return View(reservation);
        }

        // POST: Reservation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReservationId,UserId,CarId,ReservationDate,StartDate,EndDate,TotalPrice,FromCity, ToCity, Status, PickupLocationId, DropoffLocationId")] Reservation input)
        {
            if (id != input.ReservationId)
            {
                return NotFound();
            }

            // Load Entity gốc để không overwrite TenantId
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.ReservationId == id);
            if (reservation == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Không dùng _context.Update(input); -> sẽ làm TenantId = 0
                reservation.UserId = input.UserId;
                reservation.CarId = input.CarId;
                reservation.ReservationDate = input.ReservationDate;
                reservation.StartDate = input.StartDate;
                reservation.EndDate = input.EndDate;
                reservation.TotalPrice = input.TotalPrice;
                reservation.FromCity = ".";
                reservation.ToCity = ".";
                reservation.Status = input.Status;
                reservation.PickupLocationId = input.PickupLocationId;
                reservation.DropoffLocationId = input.DropoffLocationId;
                // Lưu ý không đụng vào reservation.TenantId

                // Cập nhật Payment/Car theo Status - dùng reservation thay vì input
                var payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.ReservationId == reservation.ReservationId);
                try
                {
                    if(string.Equals(reservation.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                    {

                        if(payment != null)
                        {
                            payment.Status = "Paid";
                            payment.PaymentDate = DateTime.Now;
                        }

                        // Update the car's status to "Available"
                        var car = await _context.Cars.FindAsync(reservation.CarId);
                        if (car != null)
                        {
                            car.Status = "Available";
                            car.BaseLocationId = reservation.DropoffLocationId ?? car.BaseLocationId;
                            _context.Update(car);
                        }
                    }
                    else if (string.Equals(reservation.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        if (payment != null)
                        {
                            payment.Status = "Cancelled";
                        }
                        var car = await _context.Cars.FindAsync(reservation.CarId);
                        if (car != null)
                        {
                            car.Status = "Available";
                            _context.Update(car);
                        }

                    }
                    else
                    {
                        // Update the car's status to "Rented"
                        var car = await _context.Cars.FindAsync(reservation.CarId);
                        if (car != null)
                        {
                            car.Status = "Rented";
                            _context.Update(car);
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReservationExists(reservation.ReservationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            ViewData["CarId"] = new SelectList(_context.Cars, "CarId", "CarName", input.CarId);                               
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username", input.UserId);                          
            ViewData["PickupLocationId"] = new SelectList(_context.Locations.Where(l => l.IsActive), "LocationId", "Name", input.PickupLocationId);
            ViewData["DropoffLocationId"] = new SelectList(_context.Locations.Where(l => l.IsActive), "LocationId", "Name", input.DropoffLocationId); 
            return View(input);
        }

        // GET: Reservation/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reservation = await _context.Reservations
                .Include(r => r.Car)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ReservationId == id);
            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // POST: Reservation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reservation = await _context.Reservations.FindAsync(id);
            if (reservation != null)
            {
                _context.Reservations.Remove(reservation);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReservationExists(int id)
        {
            return _context.Reservations.Any(e => e.ReservationId == id);
        }
    }
}
