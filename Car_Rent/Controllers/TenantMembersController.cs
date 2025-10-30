using Car_Rent.Infrastructure.MultiTenancy;
using Car_Rent.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    [Authorize(Policy = "TenantOwner", AuthenticationSchemes = "MyCookieAuth")]
    public class TenantMembersController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly ITenantProvider _tenant;
        private readonly IBranchScopeProvider _branch;

        public TenantMembersController(CarRentalDbContext context, ITenantProvider tenant, IBranchScopeProvider branch)
        {
            _context = context;
            _tenant = tenant;
            _branch = branch;
        }

        public async Task<IActionResult> Index()
        {
            var members = await _context.TenantMemberships
                .Include(m => m.User)
                .Where(m => m.TenantId == _tenant.TenantId)
                .OrderBy(m => m.User.Username)
                .ToListAsync();

            return View(members);
        }

        public IActionResult Add() => View();


        // Add a staff to tenant
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(string usernameOrEmail, string role = "Staff")
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail))
            {
                ModelState.AddModelError("", "Please input username or email");
                return View();
            }

            // Tìm user theo email/username
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError("", "Cannot find any username or email valid with input");
                return View();
            }

            // Kiểm tra xem đã là member chưa
            bool exists = await _context.TenantMemberships
                .AnyAsync(m => m.TenantId == _tenant.TenantId && m.UserId == user.UserId);

            if (exists)
            {
                ModelState.AddModelError("", "This user already exists in tenant");
                return View();
            }

            

            _context.TenantMemberships.Add(new TenantMemberships
            {
                TenantId = _tenant.TenantId,
                UserId = user.UserId,
                Role = string.IsNullOrWhiteSpace(role) ? "Staff" : role.Trim()
            });

            _context.UserBranches.Add(new UserBranch
            {
                UserId = user.UserId,
                TenantId = _tenant.TenantId,
                LocationId = _branch.BranchId!.Value
            });

            user.UserId = 7;

            await _context.SaveChangesAsync();
            TempData["AddStaffToTenantSuccess"] = $"Added {user.Username} to this tenant successfully";

            return RedirectToAction("Index", "User");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int userId)
        {
            var mem = await _context.TenantMemberships
                .FirstOrDefaultAsync(m => m.TenantId == _tenant.TenantId && m.UserId == userId);

            if(mem != null)
            {
                _context.TenantMemberships.Remove(mem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
