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

        public ReservationController(CarRentalDbContext context)
        {
            _context = context;
        }



        // GET: Reservation
        public async Task<IActionResult> Index(string search, string? sortBy = "DateDesc", int page = 1, int pageSize = 3)
        {
            //var carRentalDbContext = _context.Reservations.Include(r => r.Car).Include(r => r.User);
            //return View(await carRentalDbContext.ToListAsync());

            var query = _context.Reservations
                .Include(r => r.Car)
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.User.Username.Contains(search) || r.Car.CarName.Contains(search) ||
                                    r.FromCity.Contains(search) || r.ToCity.Contains(search) || r.TotalPrice.ToString().Contains(search));
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
        public async Task<IActionResult> Create([Bind("ReservationId,UserId,CarId,ReservationDate,StartDate,EndDate,TotalPrice,FromCity, ToCity, Status")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
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
            return View(reservation);
        }

        // POST: Reservation/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ReservationId,UserId,CarId,ReservationDate,StartDate,EndDate,TotalPrice,FromCity, ToCity, Status")] Reservation reservation)
        {
            if (id != reservation.ReservationId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reservation);
                    await _context.SaveChangesAsync();
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
                return RedirectToAction(nameof(Index));
            }
            ViewData["CarId"] = new SelectList(_context.Cars, "CarId", "CarName", reservation.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username", reservation.UserId);
            return View(reservation);
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
