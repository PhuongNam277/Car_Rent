using System.Security.Claims;
using Car_Rent.Models;
using Car_Rent.ViewModels.Tenant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    [Authorize(AuthenticationSchemes = "MyCookieAuth", Roles ="Admin,Staff,Owner")]
    public class TenantController : Controller
    {
        private readonly CarRentalDbContext _context;
        private const string CookieScheme = "MyCookieAuth";
        public TenantController(CarRentalDbContext context)
        {
            _context = context;
        }

        // Helper: lấy UserId từ Claims
        private int? GetUserId()
        {
            var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("UserId")?.Value;
            return int.TryParse(id, out var uid) ? uid : null;
        }

        // Helper: lấy role từ Claims
        private string? GetUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value;
        }

        // Helper: set/đổi claim tenant_id và re-signin
        private async Task SetTenantClaimAsync(int tenantId, string role)
        {
            var identity = (ClaimsIdentity)User.Identity!;

            // Remove olde tenant_id claim
            var old = identity.FindFirst("tenant_id");
            if (old != null) identity.RemoveClaim(old);

            // Add new tenant_id claim
            identity.AddClaim(new Claim("tenant_id", tenantId.ToString()));

            // Update role nếu cần (Owner, Admin, Staff)
            var oldRole = identity.FindFirst(ClaimTypes.Role);
            if (oldRole != null && oldRole.Value != role)
            {
                identity.RemoveClaim(oldRole);
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            // giữ nguyên các Claim khác, chỉ cập nhật cookie
            await HttpContext.SignInAsync(
                CookieScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(8)
                }
            );
        }

        // GET: /Tenant/Select?returnUrl=...
        // Màn hình chọn tenant khi user có nhiều tenant (hoặc chưa chọn).
        public async Task<IActionResult> Select(string? returnUrl = "/")
        {
            var uid = GetUserId();
            if (uid is null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Kiểm tra role - Customer không được vào đây
            var role = GetUserRole();
            if(role == "Customer" || role == "User")
            {
                TempData["ErrorMessage"] = "Customers cannot access tenant management.";
                return RedirectToAction("Index", "Main");
            }

            var memberships = await _context.TenantMemberships
                .Include(m => m.Tenant)
                .Where(m => m.UserId == uid.Value)
                .OrderBy(m => m.Tenant.Name)
                .ToListAsync();

            // Không có tenant nào -> mời tạo mới
            if (memberships.Count == 0)
            {
                // Chỉ Owner mới được tạo tenant mới
                if(role == "Owner" || role == "Admin")
                {
                    return RedirectToAction(nameof(Create), new { returnUrl });
                }

                TempData["ErrorMessage"] = "You don't belong to any tenant. Contact administrator.";
                return RedirectToAction("Index", "Main"); 
            }

            // Nếu chỉ có 1 tenant và chưa có claim -> gắn luôn và quay về
            var hasTenantClaim = User.HasClaim(c => c.Type == "tenant_id");
            if (memberships.Count == 1 && !hasTenantClaim)
            {
                var membership = memberships[0];
                await SetTenantClaimAsync(membership.TenantId, membership.Role);
                return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
            }

            var vm = new TenantSelectViewModel
            {
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl!,
                Tenants = memberships.Select(m => new TenantItem
                {
                    TenantId = m.TenantId,
                    Name = m.Tenant.Name,
                    Role = m.Role
                }).ToList(),
                CurrentTenantId = User.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value
            };

            return View(vm);
        }

        // POST: /Tenant/Switch
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Switch(int tenantId, string? returnUrl = "/")
        {
            var uid = GetUserId();
            if (uid is null)
            {
                return RedirectToAction("Index", "Login");
            }

            // Kiểm tra role
            var role = GetUserRole();
            if(role == "Customer" || role == "User")
            {
                return Forbid();
            }

            // Kiểm tra user có membership với tenant nào hay không ?
            var membership = await _context.TenantMemberships
                .FirstOrDefaultAsync(m => m.UserId == uid && m.TenantId == tenantId);

            if (membership == null) return Forbid(); // Không cho switch nếu không thuộc tenant

            await SetTenantClaimAsync(tenantId, membership.Role);

            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl!);
        }

        // GET: /Tenant/Create
        // Tạo doanh nghiệp mới (user hiện tại sẽ là Owner)
        public IActionResult Create(string? returnUrl = "/")
        {
            var vm = new TenantCreateViewModel{ ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl! };
            return View(vm);
        }

        // POST: /Tenant/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TenantCreateViewModel model)
        {
            var uid = GetUserId();
            if (uid is null) return RedirectToAction("Index", "Login");
            if (!ModelState.IsValid) return View(model);

            // Validate: tên tenant không được trùng
            var exists = await _context.Tenants
                .AnyAsync(t => t.Name.ToLower() == model.Name.ToLower());

            if (exists)
            {
                ModelState.AddModelError("Name", "Tenant name already exists.");
                return View(model);
            }
            // Tạo tenant
            var tenant = new Tenant
            {
                Name = model.Name.Trim(),
                Status = 1,
                CreatedAt = DateTime.Now
            };
            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            // Gắn membership Owner cho user hiện tại
            var mem = new TenantMemberships
            {
                TenantId = tenant.TenantId,
                UserId = uid.Value,
                Role = "Owner"
            };
            _context.TenantMemberships.Add(mem);
            await _context.SaveChangesAsync();

            // Set claim tenant_id sang tenant mới tạo
            await SetTenantClaimAsync(tenant.TenantId, "Owner");

            var ret = string.IsNullOrWhiteSpace(model.ReturnUrl) ? "/" : model.ReturnUrl!;
            return LocalRedirect(ret);
        }

        // (Tùy chọn) Danh sách tenants mà user đang thuộc
        public async Task<IActionResult> MyTenants()
        {
            var uid = GetUserId();
            if (uid is null) return RedirectToAction("Index", "Login");

            // Kiểm tra role
            var role = GetUserRole();
            if(role == "Customer" || role == "User")
            {
                TempData["ErrorMessage"] = "Customers don't belong to any tenant.";
                return RedirectToAction("Index", "Main");
            }

            var items = await _context.TenantMemberships
                .Include(m => m.Tenant)
                .Where(m => m.UserId == uid.Value)
                .Select(m => new TenantItem
                {
                    TenantId = m.TenantId,
                    Name = m.Tenant.Name,
                    Role = m.Role
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return View(items);
        }

    }
}
