using Car_Rent.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    [Authorize(AuthenticationSchemes ="MyCookieAuth")]
    public class MyReservationController : Controller
    {
        
        private readonly CarRentalDbContext _context;

        public MyReservationController(CarRentalDbContext context)
        {
            _context = context;
        }

        // get userid from claims
        private int? GetUserId()
        {
            var claim = User.FindFirst("UserId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }


        // GET: MyReservation
        [HttpGet]
        public async Task<IActionResult> Index(string? status, DateTime? from, DateTime? to, string? route, string sortBy = "StartDateDesc", int page = 1, int pageSize  = 10)
        {
            var uid = GetUserId();
            if (uid is null) return RedirectToAction("Index", "Login");

            var q = _context.Reservations.AsNoTracking()
                .Include(r => r.Car)
                .Include(r => r.PickupLocation)
                .Include(r => r.DropoffLocation)
                .Where(r => r.UserId == uid.Value);

            if(!string.IsNullOrWhiteSpace(status)) q = q.Where(r => r.Status == status);
            if (from.HasValue) q = q.Where(r => r.StartDate >= from.Value);
            if(to.HasValue) q = q.Where(r => r.EndDate <= to.Value);
            if (!string.IsNullOrWhiteSpace(route))
            {
                var r = route.Trim();
                q = q.Where(x =>
                    (x.PickupLocation != null && x.PickupLocation.Name.Contains(r)) ||
                    (x.DropoffLocation != null && x.DropoffLocation.Name.Contains(r)) ||
                    ((x.FromCity ?? "").Contains(r)) || ((x.ToCity ?? "").Contains(r))
                );
            }

            q = sortBy switch
            {
                "StartDateAsc" => q.OrderBy(r => r.StartDate),
                "TotalPriceAsc" => q.OrderBy(r => r.TotalPrice),
                "TotalPriceDesc" => q.OrderByDescending(r => r.TotalPrice),
                _ => q.OrderByDescending(r => r.StartDate) // Default sort by StartDate descending
            };

            var total = await q.CountAsync(); 
            var items = await q.Skip((page -1) * pageSize).Take(pageSize).ToListAsync();




            ViewBag.TotalItems = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Status = status;
            ViewBag.From = from;
            ViewBag.To = to;
            ViewBag.Route = route;
            ViewBag.SortBy = sortBy;


            return View(items);
        }

        // GET: MyReservation/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            var uid = GetUserId();
            if (uid is null) return RedirectToAction("Index", "Login");

            var res = await _context.Reservations
                .Include(r => r.Car)
                .Include(r => r.Payments)
                .Include(r => r.PickupLocation)
                .Include(r => r.DropoffLocation)
                .FirstOrDefaultAsync(r => r.ReservationId == id && r.UserId == uid.Value);

            ViewBag.PickUpStationName = res?.PickupLocation?.Name ?? "N/A";
            ViewBag.DropOffStationName = res?.DropoffLocation?.Name ?? "N/A";

            if (res is null) return NotFound();
            return View(res);
        }

        // POST: /MyReservation/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason = "User cancelled from My Reservations")
        {
            var uid = GetUserId();
            if(uid is null) return RedirectToAction("Index", "Login");

            var res = await _context.Reservations.FirstOrDefaultAsync(r => r.ReservationId == id && r.UserId == uid.Value);

            if (res is null)
            {
                TempData["Error"] = "Reservation not found or you do not have permission to cancel it.";
                return RedirectToAction(nameof(Index));
            }

            // Cancel rule: not permission for cancel if reservation is already in progress, complete or cancelled
            var nonCancelable = new[] {"InProgress", "Completed", "Rejected" };
            if(res.Status != null && nonCancelable.Contains(res.Status))
            {
                TempData["Error"] = "Your reservation is already in progress, completed or rejected. You cannot cancel it.";
                return RedirectToAction(nameof(Index));
            }

            // Update status to Cancelled
            res.Status = "Cancelled";

            // Update status car to Available
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.CarId == res.CarId);
            if (car != null)
            {
                car.Status = "Available";
            }


            await _context.SaveChangesAsync();

            TempData["Success"] = "Reservation cancelled successfully.";
            return RedirectToAction(nameof(Index));

        }
    }
}
