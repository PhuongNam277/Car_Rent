using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Car_Rent.Models;
using System.Data;
using Microsoft.AspNetCore.Identity.Data;
using Car_Rent.Security;
using Car_Rent.ViewModels.Profile;

namespace Car_Rent.Controllers
{
    public class UserController : Controller
    {
        private readonly CarRentalDbContext _context;

        public UserController(CarRentalDbContext context)
        {
            _context = context;
        }
        
        // GET: User
        public async Task<IActionResult> Index(string search, string? sortBy = "UsernameAsc", int page = 1, int pageSize = 10)
        {
            var query = _context.Users.Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Username.Contains(search) || u.Email.Contains(search)
                || u.PhoneNumber.Contains(search));
            }

            // Sorting logic
            query = sortBy switch
            {
                "UsernameDesc" => query.OrderByDescending(u => u.Username),
                "CreatedDateAsc" => query.OrderBy(u => u.CreatedDate),
                "CreatedDateDesc" => query.OrderByDescending(u => u.CreatedDate),
                _ => query.OrderBy(u => u.Username)
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

        // GET: User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            return View(new User());
        }

        // POST: User/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,FullName,Username,PasswordHash,Email,PhoneNumber,RoleId,CreatedDate")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // POST: User/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserId,FullName,Username,PasswordHash,Email,PhoneNumber,RoleId,CreatedDate")] User user)
        {
            if (id != user.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserId))
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
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", user.RoleId);
            return View(user);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.UserId == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        // GET: User/ResetPassword/5
        public async Task<IActionResult> ResetPassword(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id.Value);
            if (user == null) return NotFound();

            ViewBag.UserId = user.UserId;
            ViewBag.Username = user.Username;
            return View(new RequestResetPassword());
        }


        //POST: User/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, RequestResetPassword request)
        {
            if (id <= 0 || request == null) return BadRequest("Invalid user ID or request data.");

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound("User not found.");

            if (!ModelState.IsValid)
            {
                // set lại để view có route id & title
                ViewBag.UserId = id;
                ViewBag.Username = user.Username;
                return View(request);
            }

            // Confirm khớp (dù đã có [Compare], mình vẫn nên guard phía server)
            if (request.NewPassword != request.ConfirmNewPassword)
            {
                ModelState.AddModelError(nameof(request.ConfirmNewPassword), "Passwords do not match.");
                ViewBag.UserId = id;
                ViewBag.Username = user.Username;
                return View(request);
            }

            user.PasswordHash = PasswordHasherUtil.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Password reset successfully.";
            return RedirectToAction(nameof(Index));
        }


    }
}
