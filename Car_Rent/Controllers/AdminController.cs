using System.Security.Claims;
using Car_Rent.Infrastructure.MultiTenancy;
using Car_Rent.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Car_Rent.Controllers
{
    [Authorize(Policy = "PlatformAdminOrTenantOwner", AuthenticationSchemes = "MyCookieAuth")]
    public class AdminController : Controller
    {
        private readonly CarRentalDbContext _context;
        private readonly ITenantProvider _tenant;
        private readonly IBranchScopeProvider _branch;

        public AdminController(CarRentalDbContext context, ITenantProvider tenant, IBranchScopeProvider branch)
        {
            _context = context;
            _tenant = tenant;
            _branch = branch;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Staff cần chọn chi nhánh nếu chưa có branch_id
        //[AllowAnonymous]
        //public async Task<IActionResult> SelectBranch(string? returnUrl = "/Admin")
        //{
        //    if (!_branch.IsBranchScoped) return Redirect(returnUrl ?? "/Admin"); // Owner/Admin không cần

        //    if (_branch.BranchId.HasValue) return Redirect(returnUrl ?? "/Admin");

        //    var branches = await _context.Locations
        //        .Where(b => b.TenantId == _tenant.TenantId)
        //        .OrderBy(b => b.Name)
        //        .Select(b => new BranchVm { BranchId = b.LocationId, Name = b.Name })
        //        .ToListAsync();

        //    return View(branches); // Views/Admin/SelectBranch.cshtml
        //}

        [AllowAnonymous]
        public async Task<IActionResult> SelectBranch(string? returnUrl = "/Admin")
        {
            // Nếu đã có branch rồi thì redirect về returnUrl
            if (_branch.BranchId.HasValue)
                return Redirect(returnUrl ?? "/Admin");

            // Nếu là Admin (không cần branch scope) thì redirect luôn
            if (User.IsInRole("Admin"))
                return Redirect(returnUrl ?? "/Admin");

            // Lấy danh sách branch để Owner chọn
            var branches = await _context.Locations
                .Where(b => b.TenantId == _tenant.TenantId)
                .OrderBy(b => b.Name)
                .Select(b => new BranchVm { BranchId = b.LocationId, Name = b.Name })
                .ToListAsync();

            ViewBag.ReturnUrl = returnUrl;
            return View(branches);
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult SetBranch(int branchId, string? returnUrl = "/Admin")
        {
            // Lưu branchId vào session hoặc cookie
            HttpContext.Session.SetInt32("SelectedBranchId", branchId);
            // Hoặc: Response.Cookies.Append("BranchId", branchId.ToString());

            return Redirect(returnUrl ?? "/Admin");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SwitchBranch(int branchId, string? returnUrl = "/Admin")
        {
            // Xác thực chi nhánh thuộc tenant hiện tại
            var ok = await _context.Locations.AnyAsync(b => b.TenantId == _tenant.TenantId && b.LocationId == branchId);
            if(!ok) return Forbid();

            // Set/đổi claim branch_id
            var id = (ClaimsIdentity)User.Identity!;
            var old = id.FindFirst("branch_id");
            if (old != null) id.RemoveClaim(old);
            id.AddClaim(new Claim("branch_id", branchId.ToString()));

            await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(id),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

            return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) ? "/Admin" : returnUrl);
        }

        [Authorize(Roles ="Admin", AuthenticationSchemes ="MyCookieAuth")]
        public IActionResult Role()
        {
            return View("Role", _context.Roles.ToList());
        }

        public record BranchVm
        {
            public int BranchId { get; set; }
            public string Name { get; set; } = "";
        }
    }
}
