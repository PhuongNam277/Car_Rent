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
using Microsoft.AspNetCore.Authorization;
using Car_Rent.Infrastructure.MultiTenancy;

namespace Car_Rent.Controllers
{
    [Authorize(Roles = "Owner, Admin", AuthenticationSchemes = "MyCookieAuth")]
    public class UserController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly ITenantProvider _tenant;
        private readonly IBranchScopeProvider _branch;

        public UserController(CarRentalDbContext context, ITenantProvider tenant, IBranchScopeProvider branch)
        {
            _context = context; _tenant = tenant; _branch = branch;
        }

        //Bắt buộc phải Scope theo một chi nhánh
        private IActionResult? RequireBranchScope(string returnAction)
        {
            if (User.IsInRole("Owner") && !_branch.BranchId.HasValue)
                return RedirectToAction("SelectBranch", "Admin", new { returnUrl = Url.Action(returnAction) });
            return null;
        }

        // helper: Owner chưa có branch => trả về view nhắc chọn, không redirect
        private IActionResult? RequireBranchOrPrompt(string returnAction)
        {
            if (User.IsInRole("Owner") && !_branch.BranchId.HasValue)
            {
                // Truyền returnUrl để sau khi chọn xong quay lại
                ViewBag.ReturnUrl = Url.Action(returnAction);
                return View("BranchRequired"); // ✅ KHÔNG redirect
            }
            return null;
        }

        // Kiểm tra xem user có thuộc target phạm vi hiện tại hay không
        private Task<bool> IsInScopeAsync(int userId)
        {
            if(User.IsInRole("Admin")) return Task.FromResult(true);
            int tenantId = _tenant.TenantId;
            int branchId = _branch.BranchId!.Value;

            return (from m in _context.TenantMemberships
                    join ub in _context.UserBranches
                    on new { m.UserId, m.TenantId, LocationId = branchId }
                    equals new { ub.UserId, ub.TenantId, ub.LocationId }
                    where m.UserId == userId && m.TenantId == tenantId
                    select m).AnyAsync();
        }

        // GET: User
        public async Task<IActionResult> Index(string search, string? sortBy = "UsernameAsc", int page = 1, int pageSize = 10)
        {
            //Owner phải có branch scope
            //var needScope = RequireBranchScope(nameof(Index));
            //if (needScope != null) return needScope;

            var prompt = RequireBranchOrPrompt(nameof(Index));
            if (prompt != null) return prompt;

            var query = _context.Users.Include(u => u.Role).AsQueryable();

            // nếu là Owner -> giới hạn theo tanent + branch
            if(User.IsInRole("Owner"))
            {
                int tenantId = _tenant.TenantId;
                int branchId = _branch.BranchId!.Value;

                query =
                    from u in query
                    join m in _context.TenantMemberships.AsNoTracking() on u.UserId equals m.UserId
                    join ub in _context.UserBranches.AsNoTracking()
                    on new { u.UserId, TenantId = tenantId, LocationId = branchId }
                    equals new { ub.UserId, ub.TenantId, ub.LocationId }
                    where m.TenantId == tenantId
                    select u;
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Username.Contains(search) || u.Email.Contains(search)
                || u.PhoneNumber!.Contains(search));
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

            // Chặn Owner xem user ngoài phạm vi
            if (User.IsInRole("Owner") && !await IsInScopeAsync(id.Value)) return Forbid();

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
            // Admin không cần branch, Owner cần
            var prompt = RequireBranchOrPrompt(nameof(Index));
            if (prompt != null) return prompt;

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
            // Owner cần branch scope
            var needScope = RequireBranchScope(nameof(Index));
            if (needScope != null) return needScope;

            if (ModelState.IsValid)
            {
                // Giữ nguyên hành vi thêm user
                if(user.CreatedDate == default) user.CreatedDate = DateTime.UtcNow;
                _context.Add(user);
                await _context.SaveChangesAsync();

                // Nếu là owner -> gán vào tenant + branch hiện tại (Staff theo mặc định)
                //if(User.IsInRole("Owner"))
                //{
                //    int tenantId = _tenant.TenantId;
                //    int branchId = _branch.BranchId!.Value;

                //    if(!await _context.TenantMemberships.AnyAsync(m => m.TenantId == tenantId && m.UserId == user.UserId))
                //    {
                //        _context.TenantMemberships.Add(new TenantMemberships
                //        {
                //            TenantId = tenantId,
                //            UserId = user.UserId,
                //            Role = "Staff"
                //        });
                //    }

                //    if(!await _context.UserBranches.AnyAsync(ub => ub.UserId == user.UserId && ub.TenantId == tenantId && ub.LocationId == branchId))
                //    {
                //        _context.UserBranches.Add(new UserBranch
                //        {
                //            UserId = user.UserId,
                //            TenantId = tenantId,
                //            LocationId = branchId
                //        });
                //    }

                //    await _context.SaveChangesAsync();
                //}

                return RedirectToAction("Index");
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

            // Chặn ngoài phạm vi khi là Owner
            if(User.IsInRole("Owner") && !await IsInScopeAsync(id.Value)) return Forbid();

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

            // Chặn ngoài phạm vi khi là Owner
            if (User.IsInRole("Owner") && !await IsInScopeAsync(id)) return Forbid();

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

            if (User.IsInRole("Owner") && !await IsInScopeAsync(id.Value)) return Forbid();

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
            if (User.IsInRole("Owner") && !await IsInScopeAsync(id)) return Forbid();
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
            if (User.IsInRole("Owner") && !await IsInScopeAsync(id.Value)) return Forbid();

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

            if (User.IsInRole("Owner") && !await IsInScopeAsync(id)) return Forbid();

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "MyCookieAuth", Roles = "Admin, Owner")]
        public async Task<IActionResult> Block(int id)
        {
            if (User.IsInRole("Owner") && !await IsInScopeAsync(id)) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // not allow block self
            var currentUserIdStr = User.FindFirst("UserId")?.Value;
            if (int.TryParse(currentUserIdStr, out var currentUserId) && currentUserId == id)
            {
                TempData["Error"] = "You cannot block yourself.";
                return RedirectToAction(nameof(Index));
            }

            if (!user.IsBlocked)
            {
                user.IsBlocked = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"User '{user.Username}' has been blocked successfully.";
            }
            else
            {
                TempData["Info"] = $"User '{user.Username}' has already been blocked.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(AuthenticationSchemes = "MyCookieAuth", Roles = "Admin,Owner")]
        public async Task<IActionResult> Unblock (int id)
        {
            if (User.IsInRole("Owner") && !await IsInScopeAsync(id)) return Forbid();
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if(user.IsBlocked)
            {
                user.IsBlocked = false;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"User '{user.Username}' has been unblocked successfully.";
            }
            else
            {
                TempData["Info"] = $"User '{user.Username}' is not blocked.";
            }
            return RedirectToAction(nameof(Index));
        }

    }
}
